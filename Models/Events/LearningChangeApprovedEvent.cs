namespace SFA.DAS.CommitmentsV2.Messages.Events;

// Local copy of Approvals' event (das-commitments), shaped as Learning's copy.
public class LearningChangeApprovedEvent : LearningChangeEvent { }

public class LearningChangeEvent
{
    public Guid LearningKey { get; set; }
    public long ApprenticeshipId { get; set; }
    public List<Change> Changes { get; set; } = new();

    public class Change
    {
        public string ChangeType { get; set; }
        public ChangeData Data { get; set; }
    }

    public class ChangeData
    {
        public string Old { get; set; }
        public string New { get; set; }
    }
}
