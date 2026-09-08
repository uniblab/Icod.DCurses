# Unicode Width Data Generator

`Icod.DCurses.UnicodeWidthGenerator` is a maintainer tool used to turn pinned
Unicode Character Database inputs into compact C# range data for the DCurses
terminal-width implementation.

The `0.3.0` line is intentionally pinned to **Unicode 17.0.0**. The generator
rejects another Unicode version rather than silently changing the library's
terminal-width contract.

## East Asian Width data

The authoritative input is:

```text
https://www.unicode.org/Public/17.0.0/ucd/EastAsianWidth.txt
```

After obtaining that file, run from the repository root:

```text
dotnet run --project tools/unicode-width-generator/Icod.DCurses.UnicodeWidthGenerator.csproj -- \
    path/to/EastAsianWidth.txt \
    src/Internal/Generated/UnicodeEastAsianWidthData.Generated.cs
```

The generated source contains:

- the Unicode version marker;
- merged `East_Asian_Width=A` ranges;
- merged `East_Asian_Width=W` / `F` ranges;
- Unicode's normative default-wide unassigned CJK/Plane 2/Plane 3 ranges.

## Emoji property data

The authoritative input is:

```text
https://www.unicode.org/Public/17.0.0/ucd/emoji/emoji-data.txt
```

Generate the terminal emoji-candidate table with:

```text
dotnet run --project tools/unicode-width-generator/Icod.DCurses.UnicodeWidthGenerator.csproj -- \
    --emoji \
    path/to/emoji-data.txt \
    src/Internal/Generated/UnicodeEmojiData.Generated.cs
```

This mode retains and merges only the normative Unicode `Emoji` binary property.
DCurses uses that table when deciding whether VS16 or an emoji-ZWJ text element
has emoji presentation semantics. It replaces the earlier broad hand-written
scalar-range heuristic.

## Build isolation

Generated source is checked into the repository. Normal builds and package
restores do **not** download Unicode data and do not depend on network access.

The generator deliberately remains outside `Icod.DCurses.sln`, like the package
verification tools. Its output is part of the library source and is therefore
compiled and tested by the ordinary repository matrix.
