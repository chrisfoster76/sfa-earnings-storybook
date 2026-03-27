-- Wipe script for SFA.DAS.Funding.ApprenticeshipEarnings.Database

-- EnglishAndMaths children
DELETE FROM [Domain].[EnglishAndMathsInstalment];
DELETE FROM [Domain].[EnglishAndMathsPeriodInLearning];

-- ApprenticeshipEarningsProfile children
DELETE FROM [Domain].[EnglishAndMaths];
DELETE FROM [Domain].[ApprenticeshipInstalment];
DELETE FROM [Domain].[ApprenticeshipAdditionalPayment];
DELETE FROM [History].[ApprenticeshipEarningsProfileHistory];

-- ApprenticeshipEpisode children
DELETE FROM [Domain].[ApprenticeshipEarningsProfile];
DELETE FROM [Domain].[ApprenticeshipEpisodePrice];
DELETE FROM [Domain].[ApprenticeshipPeriodInLearning];

-- ApprenticeshipLearning children then root
DELETE FROM [Domain].[ApprenticeshipEpisode];
DELETE FROM [Domain].[ApprenticeshipLearning];

-- -------------------------------------------------------
-- Short Course: deepest leaves first
-- -------------------------------------------------------

-- ShortCourseEarningsProfile children
DELETE FROM [Domain].[ShortCourseInstalment];
DELETE FROM [History].[ShortCourseEarningsProfileHistory];

-- ShortCourseEpisode children then episode then root
DELETE FROM [Domain].[ShortCourseEarningsProfile];
DELETE FROM [Domain].[ShortCourseEpisode];
DELETE FROM [Domain].[ShortCourseLearning];
