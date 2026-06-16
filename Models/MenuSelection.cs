namespace LearnerDataStorybook.Models;

public abstract record MenuSelection;
public record QuitSelection : MenuSelection;
public record RunStorySelection(StoryEntry Entry) : MenuSelection;
public record RunAllSelection(List<StoryEntry> Stories) : MenuSelection;
