namespace CanvasBot;

public interface IAutocompleteCommand : ICommand
{
    public Task ExecuteAutocomplete(AutocompleteInteractionContext ctx);
}