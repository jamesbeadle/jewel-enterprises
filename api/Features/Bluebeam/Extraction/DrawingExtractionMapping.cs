using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Bluebeam.Extraction;

public static class DrawingExtractionMapping
{
    public static DrawingExtraction ToModel(this DrawingExtractionEntity entity) => new(
        entity.DrawingExtractionId,
        entity.DrawingRevisionId,
        entity.DrawingId,
        entity.ProjectId,
        (DrawingExtractionStatus)entity.Status,
        entity.QueuedBy,
        entity.QueuedAt,
        entity.StartedAt,
        entity.CompletedAt,
        entity.Attempts,
        entity.ErrorMessage,
        entity.PageCount,
        entity.MarkupCount,
        entity.MarkupsNote,
        entity.DimensionCount,
        entity.CalloutCount,
        entity.ShapeCount,
        entity.Scale,
        entity.ScaleVerified,
        entity.DrawingNumber,
        entity.RevisionLabel);

    public static DrawingMarkup ToModel(this DrawingMarkupEntity entity) => new(
        entity.DrawingMarkupId,
        entity.BluebeamMarkupId,
        entity.PageNumber,
        entity.MarkupType,
        entity.Subject,
        entity.Author,
        entity.Comment,
        entity.Colour,
        entity.MeasurementValue,
        entity.MeasurementUnit);
}
