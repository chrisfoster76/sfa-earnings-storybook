# Earnings Storybook — Claude Context

See `C:\code\sfa\projects\earnings\utilities\earnings-storybook.md` for the story format. Claude may create and edit stories, but the user runs them.

---

## WIP — migrating legacy-created stories to the draft POST → approve route (started 2026-10-26)

### Why

A recent breaking change means Earnings only calculates for learners that meet all of these:
- the provider is in `ApprenticeshipOptInConfiguration.EarningsOptedInProviders`
- the earliest start is on or after `ApprenticeshipOptInConfiguration.StartDate` (2026-08-01)
- the learner was POSTed as a draft and then approved

`ApproveLearningCommandHandler` returns early when there's no draft. So a story that creates the learner only through the legacy `ApprenticeshipCreatedEvent` gets no earnings, and every Earnings assertion reads 0.

### Pattern (applied per story)

1. New step 1: POST a draft to `/providers/10005077/learners` with the original plan (one `onProgramme` item, one cost, no break), with `delayMs: 3000`.
2. Step 2: the existing `ApprenticeshipCreatedEvent`, renamed "Employer Approves the Draft Apprenticeship".
3. Steps 3–4: "Get Learner Key" (keyed on `ApprovalsApprenticeshipId`, unchanged) and then the PUT.
4. Renumber the payloads 01–04.
5. Date shift:
   - 2024-10-01 stories: **+2 years**. The calendar months stay the same, so `DeliveryPeriod` assertions still hold, and the window still has no 29 Feb.
   - 2025-08-01 stories: **+1 year**. The academic-year assertions go up by one (2526→2627, 2627→2728).
6. The expected amounts stay the same.

Template: `stories/Apprenticeships/ApprovalsIntegration/apprenticeship-approvals-start-date-change-auto-approved`.

### Progress — `Apprenticeships/BreakInLearning` group (9 stories)

- [~] `apprenticeship-break-in-learning`: converted. The user ran it and assertions 1, 2 and 4 passed. **Assertion 3 (1 Completion + 1 Balancing in AY 2728) FAILS.**
- [ ] `apprenticeship-bil-completion-after-qp`
- [ ] `apprenticeship-bil-completion-before-qp`
- [ ] `apprenticeship-bil-pause-after-qp`
- [ ] `apprenticeship-bil-pause-before-qp`
- [ ] `apprenticeship-bil-price-change`
- [ ] `apprenticeship-bil-withdrawal-30d`
- [ ] `apprenticeship-bil-withdrawal-7d`
- [ ] `apprenticeship-bil-withdrawal-after-qp`

### Resume here: the missing Completion instalment

What the DB showed after the run:
- 12 × £266.67 Regular in 2627, 6 × £266.67 Regular in 2728 (periods 4–9), and 1 × £1,600 Balancing in 2728 period 10.
- **No Completion row.**
- The episode has `CompletionDate` 2028-05-01 and `AchievementDate` NULL.

Cause: the completion instalment is now generated from **`AchievementDate`**, not `CompletionDate` (`das-funding-apprenticeship-earnings` `ApprenticeshipEpisode.cs:98`, introduced in FLP-1377). The PUT payload has no `achievementDate`. SLD sends it per `onProgramme` item; see `stories/Apprenticeships/apprenticeship-completion-with-achievement/payloads/03-put-completion-with-achievement.json` (`"achievementDate": "2027-09-15"`).

Puzzle: the "Total £8000" assertion reportedly passed, but the rows above add up to £6,400 (3,200 + 1,600 + 1,600). Check this when resuming; maybe that assertion also failed and only #3 was reported.

Next steps:
1. Decide what achievement date to add to the post-break item of `04-put-break-and-completion.json` (probably on or after the 2028-05-01 completion), and which period the Completion assertion should then expect.
2. Re-run, then roll the pattern out to the other 8.
3. Expect the same `achievementDate` gap in `apprenticeship-bil-completion-after-qp` (asserts Completion £3000 in period 12) and in `apprenticeship-bil-price-change`.

### `Apprenticeships/IncentiveEligibility` group (11 stories): converted 2026-11-05, not yet run

- The same pattern, applied by script.
- The original start was 2026-04-01. Every date moved **+4 years** (start 2030-04-01, DOB 2012-01-01), because +4 is the only shift that keeps the leap-year pattern; 2028-02-29 falls inside the 365-day BiL post-break window.
  - Day counts and delivery periods are unchanged.
  - Academic years: 2526→2930, 2627→3031, 2728→3132 in the SQL files and step names.
- The draft POST mirrors the original plan: 2030-04-01 to 2032-03-31, £6,400 + £1,600.

### Other issues spotted (fix while migrating)

- **`apprenticeship-bil-withdrawal-7d`:**
  - Its description says it's a 2-year apprenticeship starting 2025-08-01, but the payloads are 1-year, starting 2024-10-01, £15k. Fix the description.
  - The assertion is malformed: `"expected": "4"` instead of a `[{field, value}]` array, and `count-1k-instalments.sql` has no `[Count]` alias.
- **`apprenticeship-bil-price-change`:**
  - The price change on return may now be sent to Approvals (`FurtherApprovalService` in das-apim-endpoints), which could withhold payments.
  - It has completion/balancing SQL files but no assertions. Decide what to do once it's migrated.
- No assertions at all in completion-before-qp, pause-after-qp, pause-before-qp or withdrawal-after-qp. Migrate them anyway; only add assertions if the user asks.
- Assumes 10005077 is in the local `EarningsOptedInProviders` (the shared config template has an empty list).

### Scope beyond this group

After BreakInLearning, look at the other stories that start with a legacy `ApprenticeshipCreatedEvent` and then assert on Earnings.
