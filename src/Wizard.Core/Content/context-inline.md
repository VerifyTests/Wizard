## Snapshot Testing (Verify)

This project uses [Verify](https://github.com/VerifyTests/Verify) for snapshot testing.

### Key concepts

- **`.verified.*` files** are the approved snapshots. They are committed to source control.
- **`.received.*` files** are the actual output from the latest test run. They are generated when a test fails (actual != expected) and are git-ignored.
- When a test fails, compare the `.received.*` file to the `.verified.*` file to understand the difference.
- **Inline snapshots** store the approved text in the test source file as a raw string literal passed to `.Snapshot(...)`, instead of in a `.verified.` file.

### Handling test failures

When a snapshot test fails:

1. Run the failing test and read the `.received.*` file that was generated.
2. Compare it to the corresponding `.verified.*` file.
3. Determine if the difference is expected (due to an intentional code change) or a bug.
   - **If expected**: copy the `.received.*` file to the `Verified:` path listed in the exception message to accept the new snapshot.
   - **If a bug**: fix the code, not the snapshot.
4. Never hand-edit `.verified.*` files to make tests pass. Always let Verify generate the correct output by running the test.
5. Never derive the verified name from the received name. The two are not always identical. Multi-targeted projects add a runtime and version suffix to the received file, and ignored parameters are kept in the received name but dropped from the verified name. Always take the destination from the `Verified:` line of the exception message.
6. Delete every `.verified.` file listed in the `Delete:` section. Those files are no longer produced by any test, usually because a test was renamed or removed.

If a snapshot differs between runs or between machines, add a scrubber to the test (`ScrubLinesContaining(...)`, `ScrubLines(...)`, `AddScrubber(...)`) rather than accepting the new output each time.

### Handling inline snapshot failures

An inline failure is reported in the `InlineNew:` or `InlineNotEqual:` section, which carries a `Source:` line with the source file path and line number instead of a file pair:

```csharp
await Verify(input)
    .Snapshot(
        """
        line1
        line2
        """);
```

1. Read the received and expected text from the `FileContent:` section of the exception message, or from the staged `Received:` and `Expected:` paths under `obj/VerifyInline/` when present.
2. If the difference is expected, replace the raw string literal at the `Source:` location with the received text verbatim. If it is a bug, fix the code instead.
3. `InlineNew` means the `.Snapshot(...)` call has no expected value yet, so the received text becomes the literal.
4. Never invent the expected text and never edit a literal other than the one at the reported `Source:` location.

Only the first target of a verification is inlined, so one exception message can carry both inline sections and file sections.

### Scrubbed values

Verify replaces non-deterministic values in snapshots with stable placeholders:

- GUIDs become `Guid_1`, `Guid_2`, etc.
- DateTimes become `DateTime_1`, `DateTime_2`, etc.
- File paths are replaced with tokens like `{SolutionDirectory}`, `{ProjectDirectory}`, `{TempPath}`.
- Custom scrubbers may replace other values.

These placeholders are intentional and ensure snapshots are deterministic across machines and runs.

### Exception message format

When a test fails, the exception message is machine-parsable with the following structure:

```
Directory: /path/to/test/project
New:
  - Received: TestClass.Method.received.txt
    Verified: TestClass.Method.verified.txt
NotEqual:
  - Received: TestClass.Method.received.txt
    Verified: TestClass.Method.verified.txt

FileContent:

New:

Received: TestClass.Method.received.txt
<file content here>

NotEqual:

Received: TestClass.Method.received.txt
<received content>
Verified: TestClass.Method.verified.txt
<verified content>
```

 * The `Directory:` line gives the base path for all relative file references.
 * `New` means no `.verified.` file exists yet (first run or new test).
 * `NotEqual` means the `.received.` and `.verified.` files differ.
 * `Delete` means a `.verified.` file is no longer produced by any test.
 * `InlineNew` and `InlineNotEqual` are the inline snapshot equivalents of `New` and `NotEqual`. They list a `Source:` line (`path:line`) rather than a received/verified pair, optionally followed by staged `Received:`, `Expected:` and `Patch:` paths.
 * The `FileContent:` section contains the actual text content for quick comparison without needing to read files separately.

See [Exception Message Format](https://github.com/VerifyTests/Verify/blob/main/docs/exception-message-format.md) for full details.

### Verified file conventions

- Encoding: UTF-8 with BOM
- Line endings: LF (not CRLF)
- No trailing newline

### Common patterns

```csharp
// Verify an object (serialized to JSON)
await Verify(myObject);

// Verify a string
await Verify("some text output");

// Verify with inline settings
await Verify(myObject)
    .ScrubLines(line => line.Contains("volatile"))
    .DontScrubDateTimes();

// Verify throws
await ThrowsTask(() => MethodThatThrows())
    .IgnoreStackTrace();

// Verify with an inline snapshot instead of a .verified. file
await Verify(myObject)
    .Snapshot(
        """
        expected text
        """);
```
