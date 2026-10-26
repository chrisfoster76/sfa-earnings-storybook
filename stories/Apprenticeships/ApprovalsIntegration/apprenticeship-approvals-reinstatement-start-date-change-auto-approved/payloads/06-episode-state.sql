SELECT
    ae.[IsRemoved] AS IsRemoved,
    ae.[IsApproved] AS IsApproved,
    ae.[FurtherApprovalNeeded] AS FurtherApprovalNeeded
FROM [ApprenticeshipEpisode] ae
WHERE ae.[ApprovalsApprenticeshipId] = 200036
