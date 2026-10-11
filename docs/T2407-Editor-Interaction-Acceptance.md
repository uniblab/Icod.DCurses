# T2407 Editor Interaction Acceptance

**Decision:** Existing interaction contracts are sufficient; no new public
gesture or capture API is required  
**Implementation head:** `80e5354bb89e2f127b54e15a811f47fb69f4206d`  
**Environment:** Linux 6.18.44 x86_64, .NET SDK 10.0.401, headless test host

The public-only editor sample now composes the existing router with the T2402
clipboard and T2404 cursor-style facades. Application code owns the choices
that should not become DCurses policy:

- a primary press over the document or its final-column scrollbar starts
  explicit capture, while release, focus loss, prompt activation, bounds
  change, and disposal cancel that capture;
- the document and scrollbar remain separate hit targets, and `Tab` remains an
  editor command rather than implicit focus traversal;
- bracketed-paste data is inserted from the routed fallback event, while key
  release cannot execute a command a second time;
- pointer-to-caret/selection mapping, scrollbar-to-document mapping, clipboard
  fallback content, and the event loop stay in the sample/application layer;
- `Ctrl+C` and `Ctrl+V` use explicit bounded session clipboard calls. Host
  denial, unavailable transport, malformed data, or an inconclusive timeout
  leaves the sample's private fallback usable without claiming unsupported;
- the visible caret is requested as a scoped steady bar. An unavailable or
  inconclusive style acquisition leaves the host style unchanged.

The reference Microsoft Edit recording informed the acceptance behavior—fixed
chrome, retained selection repaint, viewport movement, and command routing—but
did not change the Terminal/DCurses/Widgets ownership boundary.

## Verification

- focused `CursesEditorInteraction24Tests`: 7/7 passed;
- all editor-related net10 Staging tests: 42/42 passed;
- complete net10 Staging solution build: zero warnings and zero errors;
- full net10 Staging suite: 1,410 behavioral/compatibility tests passed. The
  sole failure is the intentionally unchanged 2.3 public-API fingerprint; the
  2.4 fingerprint is created and reviewed at T2408.

No interactive terminal was available in this headless environment. Therefore
this gate does not claim observed OSC 52 consent, cursor appearance, mouse
reporting, or terminal-specific focus behavior. Those remain scoped live
acceptance items rather than inferred results.
