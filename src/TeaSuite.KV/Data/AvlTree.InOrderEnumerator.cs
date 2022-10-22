using System.Collections;
using System.Collections.Generic;

namespace TeaSuite.KV.Data;

partial class AvlTree<T>
{
    private struct InOrderEnumerator : IEnumerator<T>
    {
        private readonly Node? leftMost;
        private Node? currentNode;

        public InOrderEnumerator(Node? leftMost)
        {
            this.leftMost = leftMost;
            currentNode = leftMost;
            Current = default!;
        }

        /// <inheritdoc/>
        public T Current { get; private set; }

        /// <inheritdoc/>
        object IEnumerator.Current => Current!;

        /// <inheritdoc/>
        public void Dispose()
        {
            // Intentionally left blank.
        }

        /// <inheritdoc/>
        public bool MoveNext()
        {
            if (currentNode == null)
            {
                // We've reached the end.
                return false;
            }

            Current = currentNode.Value;

            if (currentNode.Right == null)
            {
                // The current node does not have a right child, so we need to navigate up to find the next node in order.

                // While the current node is its parent's right child, we would already have visited the current node's
                // parent, because it comes before in order. So we need to keep moving up until we're at a node that
                // isn't its parent right child.
                while (currentNode.Parent != null && currentNode == currentNode.Parent.Right)
                {
                    currentNode = currentNode.Parent;
                }

                // The current node is now a node what was it's parents left child or the root. In case of the latter,
                // the new current node will be null, i.e. we've gone through the entire tree.
                currentNode = currentNode.Parent;
            }
            else // (next.Right != null)
            {
                // The next node is the left-most node of the right child of the next node.
                currentNode = currentNode.Right;

                while (currentNode.Left != null)
                {
                    currentNode = currentNode.Left;
                }
            }

            return true;
        }

        /// <inheritdoc/>
        public void Reset()
        {
            currentNode = leftMost;
            Current = default!;
        }
    }
}
