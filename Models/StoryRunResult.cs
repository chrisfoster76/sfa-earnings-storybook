namespace LearnerDataStorybook.Models;

public record StoryRunResult(
    bool StepsSucceeded,
    int AssertionsPassed,
    int AssertionsTotal,
    TimeSpan Duration)
{
    public bool AllPassed => StepsSucceeded && AssertionsPassed == AssertionsTotal;
}
