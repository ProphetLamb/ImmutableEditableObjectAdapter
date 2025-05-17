using System.Text;

namespace ImmutableEditableObjectAdapter;

public static class PostInitializationSource
{
    public const string EditableObjectAdapterMetadataName = "ImmutableEditableObjectAdapter`1";

    public const string EditableObjectAdapterDeclaration = """
                                                            #nullable enable
                                                            namespace System.ComponentModel
                                                            {
                                                                using global::System;
                                                                using global::System.Runtime.CompilerServices;

                                                                /// <summary>
                                                                /// Provides the old, and new value of the <see cref="EditedEventHandler{TContract}"/>, and indicates whether the value has changed.
                                                                /// </summary>
                                                                /// <typeparam name="TContract">The type of the contract <c>record</c>.</typeparam>
                                                                public sealed class EditedEventArgs<TContract> : EventArgs
                                                                {
                                                                    public EditedEventArgs(TContract oldValue, TContract newValue, bool cancelledOrUnchanged)
                                                                    {
                                                                        OldValue = oldValue;
                                                                        NewValue = newValue;
                                                                        CancelledOrUnchanged = cancelledOrUnchanged;
                                                                    }

                                                                    public TContract OldValue { get; }
                                                                    public TContract NewValue { get; }
                                                                    public bool CancelledOrUnchanged { get; }
                                                                }

                                                                /// <summary>
                                                                /// Represents the method that will handle the <see cref="ImmutableEditableObjectAdapter{TContract}.Edited"/> event of an <see cref="ImmutableEditableObjectAdapter{TContract}"/> instance.
                                                                /// </summary>
                                                                /// <typeparam name="TContract">The type of the contract <c>record</c>.</typeparam>
                                                                public delegate void EditedEventHandler<TContract>(
                                                                    ImmutableEditableObjectAdapter<TContract> sender,
                                                                    EditedEventArgs<TContract> args
                                                                )
                                                                    where TContract : notnull;

                                                                /// <summary>
                                                                /// Non-generic interface implemented by <see cref="ImmutableEditableObjectAdapter{TContract}"/>.
                                                                /// </summary>
                                                                public interface IImmutableEditableObjectAdapter : IEditableObject, INotifyPropertyChanged, INotifyPropertyChanging
                                                                {
                                                                    /// <summary>
                                                                    /// Occurs once, before <see cref="IEditableObject.EndEdit"/> replaces the immutable state <c>record</c>, or <see cref="IEditableObject.CancelEdit"/> discards changes.
                                                                    /// <br/>
                                                                    /// sender is <cref see="ImmutableEditableObjectAdapter{TContract}"/>
                                                                    /// <br/>
                                                                    /// event args is <cref see="EditedEventArgs{TContract}"/>
                                                                    /// </summary>
                                                                    void RegisterOnce(EventHandler callback);
                                                                }

                                                                /// <summary>
                                                                /// Derive a <c>sealed partial class</c> to generate a <see cref="IEditableObject"/> from a immutable state <c>record</c> type.
                                                                /// <br/>
                                                                /// Update the immutable state when the <see cref="Edited"/> event indicates the state is replaced.
                                                                /// </summary>
                                                                /// <typeparam name="TContract">The type of the contract <c>record</c>.</typeparam>
                                                                public abstract class ImmutableEditableObjectAdapter<TContract> : IImmutableEditableObjectAdapter
                                                                    where TContract : notnull
                                                                {
                                                                    private Queue<EventHandler>? _registerOnceCallbacks;

                                                                    /// <inheritdoc />
                                                                    public event PropertyChangedEventHandler? PropertyChanged;

                                                                    /// <inheritdoc />
                                                                    public event PropertyChangingEventHandler? PropertyChanging;

                                                                    /// <summary>
                                                                    /// Occurs before <see cref="EndEdit"/> replaces the immutable state <c>record</c>, or <see cref="CancelEdit"/> discards changes.
                                                                    /// </summary>
                                                                    public event EditedEventHandler<TContract>? Edited;

                                                                    /// <inheritdoc />
                                                                    void IImmutableEditableObjectAdapter.RegisterOnce(EventHandler callback)
                                                                    {
                                                                        Queue<EventHandler> registerOnceCallbacks = _registerOnceCallbacks ?? new Queue<EventHandler>();
                                                                        _registerOnceCallbacks = registerOnceCallbacks;
                                                                        registerOnceCallbacks.Enqueue(callback);
                                                                    }

                                                                    /// <inheritdoc />
                                                                    public abstract void BeginEdit();

                                                                    /// <inheritdoc />
                                                                    public abstract void CancelEdit();

                                                                    /// <inheritdoc />
                                                                    public abstract void EndEdit();

                                                                    /// <summary>
                                                                    /// Enumerate names of all changed properties during edit.
                                                                    /// </summary>
                                                                    public abstract IEnumerable<string> ChangedProperties();

                                                                    /// <summary>
                                                                    /// Indicates whether the property with the name name has changed during edit.
                                                                    /// </summary>
                                                                    public abstract bool IsPropertyChanged(string propertyName);

                                                                    protected virtual void OnPropertyChanging([CallerMemberName] string? propertyName = null)
                                                                    {
                                                                        PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));
                                                                    }

                                                                    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
                                                                    {
                                                                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                                                                    }

                                                                    protected virtual void OnEdited(TContract oldValue, TContract newValue, bool cancelledOrUnchanged)
                                                                    {
                                                                        EditedEventArgs<TContract> args = new EditedEventArgs<TContract>(oldValue, newValue, cancelledOrUnchanged);
                                                                        Edited?.Invoke(this, args);
                                                                        Queue<EventHandler>? registerOnceCallbacks = _registerOnceCallbacks;
                                                                        if (registerOnceCallbacks is not null)
                                                                        {
                                                                            while (registerOnceCallbacks.Count != 0)
                                                                            {
                                                                                EventHandler callback = registerOnceCallbacks.Dequeue();
                                                                                callback(this, args);
                                                                            }
                                                                        }
                                                                    }

                                                                    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
                                                                    {
                                                                        if (EqualityComparer<T>.Default.Equals(field, value))
                                                                        {
                                                                            return false;
                                                                        }
                                                                        OnPropertyChanging(propertyName);
                                                                        field = value;
                                                                        OnPropertyChanged(propertyName);
                                                                        return true;
                                                                    }
                                                                }
                                                            }
                                                            #nullable restore
                                                            """;

    public const string EditableExtensionsMetadataName = "EditableExtensions";

    public const string EditableExtensionsDeclaration = """
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
                                                         """;

    public static void AddSources(IncrementalGeneratorPostInitializationContext context)
    {

        context.AddSource(
            $"{nameof(EditableObjectAdapterDeclaration)}.g.cs",
            SourceText.From(EditableObjectAdapterDeclaration, Encoding.UTF8)
        );
        context.AddSource(
            $"{nameof(EditableExtensionsMetadataName)}.g.cs",
            SourceText.From(EditableExtensionsDeclaration, Encoding.UTF8)
        );
    }
}
