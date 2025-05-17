﻿using System.Diagnostics;

namespace ImmutableEditableObjectAdapter;

public static class DiagnosticsExtensions
{
    /// <summary>Indicates whether any of the diagnostics is a warning</summary>
    [DebuggerStepThrough]
    public static bool AnyWarning(in this ImmutableArray<Diagnostic> diagnostics)
    {
        foreach (var d in diagnostics)
        {
            if (d.Severity >= DiagnosticSeverity.Warning)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Indicates whether any of the diagnostics is a error</summary>
    [DebuggerStepThrough]
    public static bool AnyError(in this ImmutableArray<Diagnostic> diagnostics)
    {
        foreach (var d in diagnostics)
        {
            if ((d.Severity >= DiagnosticSeverity.Warning && d.IsWarningAsError)
                || d.Severity == DiagnosticSeverity.Error)
            {
                return true;
            }
        }

        return false;
    }
}
