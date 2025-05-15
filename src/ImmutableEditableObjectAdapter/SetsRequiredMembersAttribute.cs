using System.ComponentModel;

#pragma warning disable IDE0130
// ReSharper disable once CheckNamespace
namespace System.Diagnostics.CodeAnalysis;
#pragma warning restore IDE0130

/// <summary>
/// Specifies that this constructor sets all required members for the current type, and callers
/// do not need to set any required members themselves.
/// </summary>
[AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
#if SYSTEM_PRIVATE_CORELIB
    public
#else
internal
#endif
    sealed class SetsRequiredMembersAttribute : Attribute
{
}