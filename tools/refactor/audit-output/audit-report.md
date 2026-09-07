# Refactor audit

Generated 2026-09-07 07:46 UTC.

## Headline

**709 of 3,614 source files are over the 100-line limit (19.6%)**; the worst file is 525 lines.

## Summary

| Check | Key figures |
| --- | --- |
| fileLength | limit: 100, filesOverLimit: 709, totalFiles: 3614, worstFileLines: 525 |
| functionShape | limit: 30, functionsOverLimit: 728, elseBlocks: 1131, measurementIsHeuristic: True |
| functionNames | overlongFunctionNames: 41, maxWords: 5, maxLength: 40 |
| duplication | clones: 459, duplicatedLines: 5611, totalLines: 230181, duplicatedPercentage: 2.44 |
| naming | bannedAbbreviationHits: 618, unprefixedBooleans: 1621 |
| comments | explanatoryCommentLines: 14501, filesWithComments: 2075, taskMarkers: 51 |
| magicValues | inlineHexColours: 43, inlineStyleAttributes: 55, repeatedStringLiterals: 30 |
| prose | longMemberChainLines: 2567, deeplyIndentedLines: 2815, overlongLines: 1551, measurementIsHeuristic: True |
| inventory | pages: 98, components: 145, orphanComponents: 10, averagePageLines: 209 |

## Against the baseline

| Ratcheted figure | Baseline | Now | Verdict |
| --- | --- | --- | --- |
| fileLength.filesOverLimit | 713 | 709 | better |
| fileLength.worstFileLines | 621 | 525 | better |
| functionShape.functionsOverLimit | 731 | 728 | better |
| functionShape.elseBlocks | 1133 | 1131 | better |
| duplication.duplicatedPercentage | 2.44 | 2.44 | held |
| comments.explanatoryCommentLines | 14603 | 14501 | better |
| magicValues.inlineHexColours | 43 | 43 | held |
| inventory.orphanComponents | 10 | 10 | held |
| prose.longMemberChainLines | 2569 | 2567 | better |
| prose.deeplyIndentedLines | 2834 | 2815 | better |
| functionNames.overlongFunctionNames | 41 | 41 | held |

## Worst files by length

| File | Lines |
| --- | --- |
| jpms/Pages/SalesStrategyDetail.razor | 525 |
| jpms/Pages/SalesLeadDetail.razor | 505 |
| worker/MailboxIntake/Graph/GraphMailClient.cs | 475 |
| jpms/Pages/Imagine.razor | 464 |
| jpms/Pages/SalesInbox.razor | 457 |
| api/Data/JpmsContext.Model.cs | 449 |
| jpms/Services/Navigation/SidebarFolders.cs | 449 |
| jpms/Pages/AdminKpis.razor | 429 |
| jpms/Components/ManualWorkOrderModal.razor.cs | 428 |
| jpms/Pages/CostCodes.razor | 415 |
| jpms/Services/HttpLabourStore.cs | 407 |
| api/Features/Commercial/Documents/ValuationReportSnapshotRenderer.Sections.cs | 402 |
| jpms/Pages/TriageQueue.razor | 399 |
| jpms/Features/Triage/AttachmentPicker.razor | 396 |
| jpms/Pages/ProjectVariations.razor | 396 |
| jpms/Components/ValuationReportTable.razor | 395 |
| api/Features/Ai/Sources/AiFiledDocuments.cs | 394 |
| jpms/Pages/ProjectValuation.razor | 388 |
| api/Features/Procurement/Commands/ExtractTenderFromMessageHandler.cs | 381 |
| api/Features/Subcontractors/Documents/SubcontractorStatementRenderer.cs | 378 |

Full detail, including every offender list, is in `audit.json`.
