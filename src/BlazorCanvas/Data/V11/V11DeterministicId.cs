using System.Buffers.Binary;

namespace BlazorCanvas.Data.V11;

// D-62 requires the old-id-to-uuid mapping to be computed once and deterministically. Derivation
// makes migration re-runnable: a replay produces the same primary keys, so duplicate inserts become
// no-ops, and the 10-04 replay test can name expected ids instead of discovering them.
public static class V11DeterministicId
{
    // These prefixes must never change: changing either would re-key every already-migrated row.
    private static readonly byte[] CanvasPrefix = [0x43, 0x41, 0x4E, 0x56, 0x41, 0x53]; // "CANVAS"
    private static readonly byte[] FigurePrefix = [0x46, 0x49, 0x47, 0x55, 0x52, 0x45]; // "FIGURE"

    // These identifiers are predictable by design, not capability tokens. Every read and write path
    // filters by canvas_id, so predictability never provides access to another user's figures. Any
    // future use of figure UUIDs as unguessable secrets must replace this derivation first.
    public static Guid ForCanvas(int legacyUserId)
    {
        if (legacyUserId < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(legacyUserId));
        }

        return Create(CanvasPrefix, legacyUserId);
    }

    public static Guid ForFigure(int legacyFigureId)
    {
        if (legacyFigureId < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(legacyFigureId));
        }

        return Create(FigurePrefix, legacyFigureId);
    }

    private static Guid Create(byte[] prefix, int legacyId)
    {
        var bytes = new byte[16];
        prefix.CopyTo(bytes, 0);
        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x80); // RFC 9562 custom-layout version 8.
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80); // RFC variant 0b10.
        BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(12), legacyId);

        // The little-endian overload renders a different string for these bytes; this choice is
        // therefore load-bearing for the migration replay test's expected UUID literals.
        return new Guid(bytes, bigEndian: true);
    }
}
