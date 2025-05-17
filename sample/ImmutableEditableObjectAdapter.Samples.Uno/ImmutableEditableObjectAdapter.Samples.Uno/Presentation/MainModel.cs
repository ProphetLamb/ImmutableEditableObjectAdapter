using System.ComponentModel;

namespace ImmutableEditableObjectAdapter.Samples.Uno.Presentation;

public partial record MainModel
{
    private INavigator _navigator;

    public MainModel(
        IStringLocalizer localizer,
        IOptions<AppConfig> appInfo,
        INavigator navigator)
    {
        _navigator = navigator;
        Title = "Main";
        Title += $" - {localizer["ApplicationName"]}";
        Title += $" - {appInfo?.Value?.Environment}";
    }

    public string? Title { get; }

    public IState<string> Name => State<string>.Value(this, () => string.Empty);

    public async Task GoToSecond()
    {
        var name = await Name;
        await _navigator.NavigateViewModelAsync<SecondModel>(this, data: new Entity(name!));
    }

    public IState<Person> LastEdited => State<Person>.Empty(this); 
    
    public IListState<Person> Persons => ListState.Value(this, IImmutableList<Person> () => [
        new("Max", "Green", DateTimeOffset.Now.AddYears(-43), null),
        new("Günter", "Orange", DateTimeOffset.Now.AddYears(-32), null),
    ]);

    public async Task PersonChanged(EditedEventArgs<Person> edited)
    {
        if (edited.CancelledOrUnchanged)
        {
            return;
        }

        await LastEdited.UpdateAsync(_ => edited.NewValue);
    }
}
