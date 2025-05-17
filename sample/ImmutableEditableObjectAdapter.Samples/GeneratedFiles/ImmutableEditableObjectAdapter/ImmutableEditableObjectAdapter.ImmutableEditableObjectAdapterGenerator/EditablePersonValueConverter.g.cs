
#nullable enable

#if HAS_UNO || HAS_PRESENTATION_CORE
internal sealed partial class EditablePersonValueConverter : global::Microsoft.UI.Xaml.Data.IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, string language)
    {
        return value switch
        {
            null => null,
            global::Person x => EditablePersonExtensions.ToEditable(x),

            #if HAS_UNO
            global::System.Collections.Immutable.IImmutableList<global::Person> x => EditablePersonExtensions.ToEditableList(x),
            global::Uno.Extensions.Reactive.IFeed<global::Person> x => EditablePersonExtensions.ToEditableFeed(x),
            global::Uno.Extensions.Reactive.IFeed<global::System.Collections.Immutable.IImmutableList<global::Person>> x => EditablePersonExtensions.ToEditableListFeed(x.AsListFeed()).AsFeed(),
            global::Uno.Extensions.Reactive.IListFeed<global::Person> x => EditablePersonExtensions.ToEditableListFeed(x),

            #endif
            _ => throw new NotSupportedException($"{nameof(EditablePersonValueConverter)} can only convert {nameof(global::Person)} to {nameof(EditablePerson)} types, as well as immutable lists and feeds thereof."),
        };        

    }    

    public object? ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        return value switch
        {
            null => null,
            EditablePerson x => x.Unedited,

            #if HAS_UNO
            global::System.Collections.Immutable.IImmutableList<EditablePerson> l => (global::System.Collections.Immutable.IImmutableList<global::Person>)l.Select(x => x.Unedited).ToImmutableArray(),
            global::Uno.Extensions.Reactive.IFeed<EditablePerson> f => f.Select(x => x.Unedited),
            global::Uno.Extensions.Reactive.IFeed<global::System.Collections.Immutable.IImmutableList<EditablePerson>> f => f.Select(global::System.Collections.Immutable.IImmutableList<global::Person> (l) => l.Select(x => x.Unedited).ToImmutableArray()),
            global::Uno.Extensions.Reactive.IListFeed<EditablePerson> f => f.AsFeed().Select(global::System.Collections.Immutable.IImmutableList<global::Person> (l) => l.Select(x => x.Unedited).ToImmutableArray()).AsListFeed(),

            #endif
            _ => throw new NotSupportedException($"{nameof(EditablePersonValueConverter)} can only convert {nameof(EditablePerson)} back to {nameof(global::Person)} types, as well as immutable lists and feeds thereof."),
        };        

    }    

}


#endif

#nullable restore
