using Microsoft.UI.Xaml.Data;

namespace ImmutableEditableObjectAdapter.Samples.Uno.Converters;

[ImmutableEditableValueConverter(typeof(EditablePerson))]
public sealed partial class EditablePersonValueConverter : IValueConverter;
