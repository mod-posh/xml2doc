using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xml2Doc.Sample
{
    /// <summary>
    /// Demonstration types whose names exercise different slug/anchor algorithms (Default, Github/Gfm, Kramdown).
    /// </summary>
    /// <remarks>
    /// Each class name intentionally includes characters or patterns that various slug generators treat differently:
    /// <list type="bullet">
    ///   <item><description>Mixed case + underscores + digits (<see cref="HTTP_Parser_v2"/>).</description></item>
    ///   <item><description>Nested type (introduces a '+' in doc IDs) with generic arity and double underscores (<see cref="Outer.Inner_Type__Beta2{T}"/>).</description></item>
    ///   <item><description>Sequences of underscores in a single identifier (<see cref="Name__With__Many___Underscores"/>).</description></item>
    ///   <item><description>Diacritics / accented characters (<see cref="RésuméParser"/>).</description></item>
    ///   <item><description>Plain ASCII baseline (<see cref="SimpleType"/>).</description></item>
    /// </list>
    /// Use these to visually compare generated anchors under different <c>AnchorAlgorithm</c> values.
    /// </remarks>
    public class HTTP_Parser_v2
    {
        /// <summary>
        /// Trivial member; included so the type has at least one documented method anchor.
        /// </summary>
        public void Go() { }
    }

    /// <summary>
    /// Outer container used solely to introduce a nested type (inner doc IDs use '+' between type names).
    /// </summary>
    public class Outer
    {
        /// <summary>
        /// Nested generic type showcasing double underscores, digits, and a generic placeholder.
        /// </summary>
        /// <typeparam name="T">Generic type parameter (unused; present to show generic arity in anchors).</typeparam>
        public class Inner_Type__Beta2<T>
        {
            /// <summary>
            /// Dummy method; exists to produce a member anchor within a nested generic type context.
            /// </summary>
            public void M() { }
        }
    }

    /// <summary>
    /// Type with multiple sequential underscore runs to highlight compression / preservation differences across slug algorithms.
    /// </summary>
    public class Name__With__Many___Underscores
    {
        /// <summary>
        /// Simple auto-property; value irrelevant, present only to add a property anchor.
        /// </summary>
        public int X { get; set; }
    }

    /// <summary>
    /// Contains diacritics (e.g. 'é', 'ú') which some algorithms strip (GitHub/Gfm/Kramdown) but the Default may reduce differently.
    /// </summary>
    public static class RésuméParser { }

    /// <summary>
    /// Plain ASCII identifier serving as a control sample (all algorithms produce identical slugs).
    /// </summary>
    public static class SimpleType { }
}
