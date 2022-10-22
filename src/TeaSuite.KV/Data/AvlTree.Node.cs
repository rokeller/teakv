using System.Diagnostics;
namespace TeaSuite.KV.Data;

partial class AvlTree<T>
{
    /// <summary>
    /// Represents a node in the tree.
    /// </summary>
    [DebuggerDisplay($"Value = {{{nameof(Value)}}} (BF: {{{nameof(BalanceFactor)}}})")]
    internal sealed class Node
    {
        public Node(T value, Node? parent = null, Node? left = null, Node? right = null)
        {
            Value = value;
            Parent = parent;
            Left = left;
            Right = right;

            BalanceFactor = (right?.BalanceFactor ?? 0) - (left?.BalanceFactor ?? 0);
            Debug.Assert(-1 <= BalanceFactor && BalanceFactor <= 1, "The balance factor must be between -1 and 1.");
        }

        public T Value { get; set; }
        public Node? Parent { get; internal set; }
        public Node? Left { get; internal set; }
        public Node? Right { get; internal set; }

        internal int BalanceFactor { get; set; }
    }
}
