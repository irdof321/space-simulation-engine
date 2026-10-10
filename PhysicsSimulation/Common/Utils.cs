using PhysicsSimulation.Mathematics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PhysicsSimulation.Common
{
    public static class Constants
    {
        public static double G = 6.647430e-11f;
    }


    public class SkewSymmetricMatrix<T> : IEnumerable<(T Row, T Col)> where T : class
    {
        // Holds the unique values for the strictly lower part of the matrix
        private readonly Dictionary<(T row, T col), Vector3Double> _values =
    new Dictionary<(T row, T col), Vector3Double>();

        public void RemoveWhereIsT(T key)
        {
            var keysToRemove = _values.Keys
                .Where(keyTuple => (keyTuple.row == key) || (keyTuple.col == key))
                .ToList();

            foreach (var tuple in keysToRemove)
            {
                _values.Remove(tuple);
            }
        }

        public Vector3Double this[T row, T col]
        {
            get
            {
                if (row == null || col == null) throw new ArgumentNullException("Keys cannot be null.");
                if (row == col) return new Vector3Double(0, 0, 0); // The diagonal (planet acting on itself) is always zero

                // We use GetHashCode to consistently order the keys in memory
                if (row.GetHashCode() > col.GetHashCode())
                {
                    return _values.TryGetValue((row, col), out Vector3Double val) ? val : new Vector3Double(0, 0, 0);
                }
                else
                {
                    // Inverted access: flip keys and invert the sign
                    return _values.TryGetValue((col, row), out Vector3Double val) ? -val : new Vector3Double(0, 0, 0);
                }
            }
            set
            {
                if (row == null || col == null) throw new ArgumentNullException("Keys cannot be null.");

                if (row == col)
                {
                    throw new ArgumentException("The diagonal must remain zero.");
                }

                if (row.GetHashCode() > col.GetHashCode())
                {
                    UpdateValue(row, col, value);
                }
                else
                {
                    // Inverted storage: flip keys and invert the sign
                    UpdateValue(col, row, -value);
                }
            }
        }

        private void UpdateValue(T row, T col, Vector3Double value)
        {
            var key = (row, col);
            if (value == new Vector3Double(0, 0, 0))
            {
                _values.Remove(key); // Frees memory dynamically when an acceleration drops to zero
            }
            else
            {
                _values[key] = value;
            }
        }

        // 1. Strongly-typed enumerator (Generic)
        // Yield return allows us to stream the data efficiently without creating a new list in memory
        public IEnumerator<(T Row, T Col)> GetEnumerator()
        {
            foreach (var entry in _values)
            {
                // Yield the stored strictly lower element
                yield return (entry.Key.row, entry.Key.col);
            }
        }

        // 2. Legacy non-generic enumerator (Required by the IEnumerable interface)
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int AllocatedElementsCount => _values.Count;
    }
}
