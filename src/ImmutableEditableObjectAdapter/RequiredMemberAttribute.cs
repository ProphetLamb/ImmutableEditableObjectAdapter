using System.ComponentModel;

#pragma warning disable IDE0130
// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices;
#pragma warning restore IDE0130

/// <summary>Specifies that a type has required members or that a member is required.</summary>
[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property,
    AllowMultiple = false,
    Inherited = false)]
[EditorBrowsable(EditorBrowsableState.Never)]
internal sealed class RequiredMemberAttribute : Attribute
{
}