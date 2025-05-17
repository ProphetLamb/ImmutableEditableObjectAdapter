#nullable enable
#if HAS_UNO
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
#if !NO_WCT_DATAGRID
#if HAS_UNO_WINUI
using CommunityToolkit.WinUI.UI.Controls;
#else
using CommunityToolkit.UWP.UI.Controls;
#endif
#endif
using Uno.Logging;

namespace Uno.Toolkit.UI;

public static partial class EditableExtensions
{
    private static ILogger _logger = typeof(EditableExtensions).Log();

    public static DependencyProperty CommandProperty
    {
        [DynamicDependency(nameof(GetCommand))]
        get;
    } = DependencyProperty.RegisterAttached(
        "Command",
        typeof(ICommand),
        typeof(EditableExtensions),
        new(null, OnCommandChanged));

    [DynamicDependency(nameof(SetCommand))]
    public static ICommand? GetCommand(DependencyObject obj) => (ICommand?)obj.GetValue(CommandProperty);

    [DynamicDependency(nameof(GetCommand))]
    public static void SetCommand(DependencyObject obj, ICommand? value) => obj.SetValue(CommandProperty, value);

    public static DependencyProperty CommandParameterProperty
    {
        [DynamicDependency(nameof(GetCommandParameter))]
        get;
    } = DependencyProperty.RegisterAttached(
        "CommandParameter",
        typeof(object),
        typeof(EditableExtensions),
        new(null, OnCommandChanged));

    [DynamicDependency(nameof(SetCommandParameter))]
    public static object? GetCommandParameter(DependencyObject obj) => obj.GetValue(CommandParameterProperty);

    [DynamicDependency(nameof(GetCommandParameter))]
    public static void SetCommandParameter(DependencyObject obj, object? value) => obj.SetValue(CommandParameterProperty, value);

    private static void OnCommandChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
#if !NO_WCT_DATAGRID
        if (sender is DataGrid dg)
        {
            dg.BeginningEdit -= DataGridBeginningEdit;
            dg.BeginningEdit += DataGridBeginningEdit;
            return;
        }

        _logger.WarnFormat("Sender must be a DataGrid. {0} is not supported.", sender);
#endif
    }

#if !NO_WCT_DATAGRID
    private static void DataGridBeginningEdit(object? o, DataGridBeginningEditEventArgs args)
    {
        var dg = (DataGrid)o!;
        var command = GetCommand(dg);
        if (command is null)
        {
            return;
        }

        var parameter = GetCommandParameter(dg);

        EventHandler editableOnEdited = (_, e) =>
        {
            if (command.CanExecute(parameter ?? e))
            {
                command.Execute(parameter ?? e);
            }
        };

        if (args.Row.DataContext is IImmutableEditableObjectAdapter editable)
        {
            editable.RegisterOnce(editableOnEdited);
        }
    }
#endif
}
#endif
#nullable restore