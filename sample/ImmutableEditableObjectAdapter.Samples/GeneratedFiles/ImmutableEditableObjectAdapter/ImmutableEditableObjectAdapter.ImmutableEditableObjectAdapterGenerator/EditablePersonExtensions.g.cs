
#nullable enable
internal static partial class EditablePersonExtensions
{

    #if HAS_UNO
    public static EditablePerson ToEditable(this global::Person contract, global::System.Windows.Input.ICommand? command = null)
    {
        EditablePerson editable = new EditablePerson(contract);
        if (command != null)
        {
            editable.Edited += (_, args) =>
            {
                if (command.CanExecute(args))
                {
                    command.Execute(args);
                }
                
            };
            
        }
        
        return editable;
    }
    

    #else
    public static EditablePerson ToEditable(this global::Person contract)
    {
        return new EditablePerson(contract);
    }
    

    #endif

    #if HAS_UNO
    public static global::System.Collections.Immutable.IImmutableList<EditablePerson> ToEditableList(this global::System.Collections.Immutable.IImmutableList<global::Person> contractList, global::System.Windows.Input.ICommand? command = null)
    {
        return contractList.Select(x => ToEditable(x, command)).ToImmutableArray();
    }
    

    #endif

    #if HAS_UNO
    public static global::Uno.Extensions.Reactive.IFeed<EditablePerson> ToEditableFeed(this global::Uno.Extensions.Reactive.IFeed<global::Person> feed, global::System.Windows.Input.ICommand? command = null)
    {
        return feed.Select(x => ToEditable(x, command));
    }
    

    #endif

    #if HAS_UNO
    public static global::Uno.Extensions.Reactive.IListFeed<EditablePerson> ToEditableListFeed(this global::Uno.Extensions.Reactive.IListFeed<global::Person> feed, global::System.Windows.Input.ICommand? command = null)
    {
        return feed.AsFeed().Select(x => ToEditableList(x, command)).AsListFeed();
    }
    

    #endif
}


#nullable restore
