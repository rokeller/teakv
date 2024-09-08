using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace TeaSuite.KV.Data;

/// <summary>
/// Implements an AVL tree.
/// </summary>
/// <typeparam name="T">
/// The type of values for tree nodes.
/// </typeparam>
[DebuggerDisplay($"Count = {{{nameof(Count)}}}")]
internal partial class AvlTree<T>
{
    private Node? root;
    private int count;
    private readonly IComparer<T> comparer;

    public AvlTree(IComparer<T> comparer)
    {
        this.comparer = comparer;
    }

    public int Count => count;

    /// <summary>
    /// Upserts a value in the tree. If a node with the given value already
    /// exists, the value is updated. Otherwise, a new node is inserted and the
    /// tree balanced if necessary.
    /// </summary>
    /// <param name="value">
    /// The value to upsert.
    /// </param>
    /// <returns>
    /// The current <see cref="AvlTree{T}"/>.
    /// </returns>
    /// <remarks>
    /// Since value equality comparison is done using the <see cref="IComparable{T}"/>
    /// interface on the value, equality of the value does not mean that the
    /// values are completely equivalent. In the case of key/value pairs where
    /// the <see cref="IComparable{T}"/> implementation compares only the keys,
    /// obviously the values can be different.
    /// </remarks>
    public AvlTree<T> Upsert(T value)
    {
        if (root == null)
        {
            root = new(value);
            count = 1;
        }
        else if (!Upsert(root, value))
        {
            count++;
        }

        return this;
    }

    /// <summary>
    /// Tries to find the current value for the given <paramref name="key"/> in
    /// the tree.
    /// </summary>
    /// <param name="key">
    /// The key to find.
    /// </param>
    /// <param name="value">
    /// If found, contains the current value.
    /// </param>
    /// <returns>
    /// True if found, false otherwise.
    /// </returns>
    public bool TryFind(T key, out T value)
    {
        Node? node = root;

        while (node != null)
        {
            int result = comparer.Compare(key, node.Value);
            if (result < 0)
            {
                // The key to find is smaller so must be in the left subtree, if any.
                node = node.Left;
            }
            else if (result > 0)
            {
                // The key to find is bigger so must be in the right subtree, if any.
                node = node.Right;
            }
            else // (result == 0)
            {
                value = node.Value;
                return true;
            }
        }

        // We have no more nodes to look at, so we cannot find the key.
        value = key;
        return false;
    }

    /// <summary>
    /// Gets an <see cref="IEnumerator{T}"/> that enumerates all the current
    /// tree's nodes in order.
    /// </summary>
    /// <returns>
    /// An <see cref="IEnumerator{T}"/> that enumerates all the current tree's
    /// nodes in order.
    /// </returns>
    public IEnumerator<T> GetInOrderEnumerator()
    {
        // We need to find the left-most node (i.e. the smallest node) -- that
        // will be where enumeration starts.
        Node? leftMost = root;
        if (null != leftMost)
        {
            while (leftMost.Left != null)
            {
                leftMost = leftMost.Left;
            }
        }

        return new InOrderEnumerator(leftMost);
    }

    private bool Upsert(Node node, T value)
    {
        while (true)
        {
            int result = comparer.Compare(value, node.Value);

            if (result < 0)
            {
                // Insert smaller keys on the left.
                if (node.Left != null)
                {
                    node = node.Left;
                }
                else
                {
                    node.Left = new(value, node);
                    node.BalanceFactor--;
                    break;
                }
            }
            else if (result > 0)
            {
                // Insert bigger keys on the right.
                if (node.Right != null)
                {
                    node = node.Right;
                }
                else
                {
                    node.Right = new(value, node);
                    node.BalanceFactor++;
                    break;
                }
            }
            else // (result == 0)
            {
                // The balance of the tree remains unchanged (no nodes added),
                // rebalancing is not needed.
                node.Value = value;
                return true;
            }
        }

        BalanceAfterAdd(node);
        return false;
    }

    private void BalanceAfterAdd(Node node)
    {
        // The tree might be out of balance now, so check that.
        while ((node.BalanceFactor != 0) && (node.Parent != null))
        {
            if (node.Parent.Left == node)
            {
                node.Parent.BalanceFactor--;
            }
            else
            {
                node.Parent.BalanceFactor++;
            }

            node = node.Parent;

            if (node.BalanceFactor == -2)
            {
                RotateToRight(node);
                break;
            }
            else if (node.BalanceFactor == 2)
            {
                RotateToLeft(node);
                break;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RotateToRight(Node node)
    {
        // The left subtree is higher, so node.Left must not be null.
        Debug.Assert(node.Left != null, "The left subtree must not be null.");
        Node x = node.Left;

        if (x.BalanceFactor == -1)
        {
            // Rotation to right: x is going to be the new root of the subtree,
            // while node will take x's right subtree on the left; then node is
            // going to be the new right subtree of x.
            x.Parent = node.Parent;

            if (node.Parent == null)
            {
                root = x;
            }
            else
            {
                if (node.Parent.Left == node)
                {
                    node.Parent.Left = x;
                }
                else
                {
                    node.Parent.Right = x;
                }
            }

            node.Left = x.Right;

            if (node.Left != null)
            {
                node.Left.Parent = node;
            }

            x.Right = node;
            node.Parent = x;

            x.BalanceFactor = node.BalanceFactor = 0;
        }
        else
        {
            Debug.Assert(x.BalanceFactor == 1,
                         "The left subtree's balance factor must be 1.");
            Debug.Assert(x.Right != null, "The right subtree must not be null.");
            Node y = x.Right;
            // Double rotation to the right: y is going to be the new root of
            // the subtree, having x on its left and node on its right. x's
            // right subtree is replaced with y's old left subtree, and node's
            // left subtree is replaced with y's old right subtree.
            y.Parent = node.Parent;

            if (node.Parent == null)
            {
                root = y;
            }
            else
            {
                if (node.Parent.Left == node)
                {
                    node.Parent.Left = y;
                }
                else
                {
                    node.Parent.Right = y;
                }
            }

            x.Right = y.Left;
            if (x.Right != null)
            {
                x.Right.Parent = x;
            }

            node.Left = y.Right;
            if (node.Left != null)
            {
                node.Left.Parent = node;
            }

            y.Left = x;
            y.Right = node;

            x.Parent = y;
            node.Parent = y;

            if (y.BalanceFactor == -1)
            {
                x.BalanceFactor = 0;
                node.BalanceFactor = 1;
            }
            else if (y.BalanceFactor == 0)
            {
                x.BalanceFactor = 0;
                node.BalanceFactor = 0;
            }
            else // y.BalanceFactor == 1 
            {
                x.BalanceFactor = -1;
                node.BalanceFactor = 0;
            }

            y.BalanceFactor = 0;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RotateToLeft(Node node)
    {
        // The right subtree is higher, so node.Right must not be null.
        Debug.Assert(node.Right != null, "The right subtree must not be null.");
        Node x = node.Right;

        if (x.BalanceFactor == 1)
        {
            // Rotation to left: x is going to be the new root of the subtree,
            // while node will take x's left subtree on the right; then node is
            // going to be the new left subtree of x.
            x.Parent = node.Parent;

            if (node.Parent == null)
            {
                root = x;
            }
            else
            {
                if (node.Parent.Left == node)
                {
                    node.Parent.Left = x;
                }
                else
                {
                    node.Parent.Right = x;
                }
            }

            node.Right = x.Left;
            if (node.Right != null)
            {
                node.Right.Parent = node;
            }

            x.Left = node;
            node.Parent = x;

            x.BalanceFactor = 0;
            node.BalanceFactor = 0;
        }
        else
        {
            Debug.Assert(x.BalanceFactor == -1,
                         "The left subtree's balance factor must be -1.");
            Debug.Assert(x.Left != null, "The left subtree must not be null.");
            Node y = x.Left;
            // Double rotation to the left: y is going to be the new root of the
            // subtree, having x on its right and node on its left. x's left
            // subtree is replaced with y's old right subtree, and node's right
            // subtree is replaced with y's old left subtree.
            y.Parent = node.Parent;

            if (node.Parent == null)
            {
                root = y;
            }
            else
            {
                if (node.Parent.Left == node)
                {
                    node.Parent.Left = y;
                }
                else
                {
                    node.Parent.Right = y;
                }
            }

            x.Left = y.Right;
            if (x.Left != null)
            {
                x.Left.Parent = x;
            }

            node.Right = y.Left;
            if (node.Right != null)
            {
                node.Right.Parent = node;
            }

            y.Right = x;
            y.Left = node;

            x.Parent = y;
            node.Parent = y;

            if (y.BalanceFactor == 1)
            {
                x.BalanceFactor = 0;
                node.BalanceFactor = -1;
            }
            else if (y.BalanceFactor == 0)
            {
                x.BalanceFactor = 0;
                node.BalanceFactor = 0;
            }
            else // (y.BalanceFactor == -1)
            {
                x.BalanceFactor = 1;
                node.BalanceFactor = 0;
            }

            y.BalanceFactor = 0;
        }
    }
}
