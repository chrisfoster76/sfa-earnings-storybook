SELECT
    SUM(CASE WHEN [Type] = 'Completion' THEN 1 ELSE 0 END) AS [CompletionCount],
    SUM(CASE WHEN [Type] = 'Balancing' THEN 1 ELSE 0 END) AS [BalancingCount]
FROM [Domain].[ApprenticeshipInstalment]
WHERE [AcademicYear] = 2627
