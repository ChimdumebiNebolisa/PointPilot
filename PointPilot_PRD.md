# PointPilot Product Requirements Document

**Product:** PointPilot  
**Version:** 1.0  
**Status:** Build Week implementation PRD  
**Owner:** Chimdumebi Mitchell-Nebolisa  
**Date:** July 15, 2026  
**Repository:** `ChimdumebiNebolisa/PointPilot`  
**Companion document:** `PROJECT_BOOTSTRAP.md`

## 1. Executive Summary

PointPilot is a Windows 11 AI companion that works beside the user inside desktop applications.

Today, using AI to understand or complete a task inside unfamiliar software usually requires manual context transfer. The user leaves the application, opens a chatbot, copies text or captures a screenshot, explains what they are trying to do, reads a detached answer, returns to the application, and translates that answer into interface actions.

PointPilot removes that loop.

During an active session, PointPilot listens continuously, sees the foreground application when context is needed, understands follow-up references such as â€œthis,â€ â€œthat panel,â€ or â€œdo it for me,â€ and responds in one of three ways:

1. **Teach:** Explain what the user is looking at and point to the relevant interface element.
2. **Guide:** Give one contextual step at a time and verify that the user completed it.
3. **Act:** Control the application through mouse and keyboard actions, verify the result, and remain interruptible throughout execution.

The product is general-purpose by architecture and positioning. The first verified actuation environment is GIMP on Windows 11 because it provides a visually clear, genuinely confusing productivity workflow that demonstrates teaching, guidance, action, visual pointing, interruption, constraint changes, and verification.

PointPilot must not be presented as â€œan AI for GIMP.â€ GIMP is the launch vertical slice and demo environment. The core architecture must remain application-agnostic.

## 2. Problem

AI chatbots are separated from the work surface they are supposed to help with.

The user must manually supply context that is already visible on their screen. This creates four forms of friction:

- **Context transfer:** Screenshots, copied text, application names, current state, and prior attempts must be described manually.
- **Translation:** The user must convert a chatbotâ€™s generic instructions into exact interface actions.
- **State loss:** The chatbot often does not know what changed after each step.
- **Mode switching:** The user repeatedly moves between learning, asking, acting, and checking.

This is especially painful in complex productivity software where meaning depends on visible and hidden application state, such as the active tool, selected layer, focused control, open dialog, current canvas, or prior action.

## 3. Product Thesis

The AI should be co-present with the user instead of living in a separate chat destination.

PointPilot should feel like a capable companion beside the active application:

- It already knows which window the user is working in.
- It captures the relevant visible context without requiring a screenshot upload.
- It understands references grounded in the current screen and conversation.
- It can point inside the live interface.
- It can teach the user or take over the task without resetting context.
- It remains available for follow-up speech without another button press.
- It can be interrupted naturally when the user changes their mind.
- It verifies the result before claiming success.

## 4. Goals

### 4.1 Primary goals

1. Deliver a complete Windows 11 desktop experience, not a command-line prototype.
2. Prove the co-present AI interaction model through a polished GIMP workflow.
3. Support continuous voice interaction during an active session.
4. Support natural barge-in while PointPilot is speaking or acting.
5. Support contextual teaching, step-by-step guidance, and direct computer control.
6. Use GPT-5.6 Computer Use meaningfully for live interface operation.
7. Verify actions through updated screenshots and deterministic checks when available.
8. Keep the feature set narrow enough that the demonstrated workflow works repeatedly.
9. Use a general-purpose core that does not depend on a GIMP API or GIMP plug-in.
10. Produce a runnable submission with clear setup, tests, and demo instructions.

### 4.2 Competition goals

The implementation and demo should make a credible case across:

- Technological implementation
- Coherent product design
- Real user impact
- Novelty and differentiation

The demo must fit within three minutes and clearly show both Codex-built implementation work and GPT-5.6 operating as a core product capability.

## 5. Non-Goals

The first version will not include:

- General reliability claims across all Windows applications
- Windows 10 support
- macOS or Linux support
- ARM64 support
- Multiple model providers
- ElevenLabs
- Deepgram
- Anthropic
- Gemini
- Local models
- Wake-word detection
- Background research agents
- Email, calendar, browser, or productivity service integrations
- MCP or Composio
- Long-term personal memory
- Learned routines or skills
- A plug-in marketplace
- A GIMP API integration
- A GIMP plug-in
- Shell or terminal execution
- Arbitrary file-system automation
- Multi-monitor support for the verified demo
- Remote Desktop or virtual-machine support
- UAC secure desktop control
- Elevated application control
- Fully autonomous destructive actions
- Continuous screen capture while no task is active

## 6. Target Users

### 6.1 Primary user

A person using unfamiliar or complex desktop productivity software who:

- Knows the outcome they want
- Does not understand the application well enough to reach it efficiently
- Would normally ask a chatbot for help
- Does not want to repeatedly explain visible context
- Sometimes wants to learn
- Sometimes wants the AI to complete the task
- May change constraints while the task is underway

### 6.2 Initial demo persona

A beginner or intermediate GIMP user creating a promotional graphic who is confused by layers, panels, filters, tool state, and export workflows.

## 7. Product Principles

### 7.1 Co-present, not detached

PointPilot should begin from the active application and current screen rather than asking the user to recreate that context in a chatbot.

### 7.2 Teach and act in one continuous context

The user must be able to move naturally between:

- â€œWhat is this?â€
- â€œShow me how.â€
- â€œActually, do it for me.â€
- â€œWait, change that.â€
- â€œUndo that.â€

The task context must survive those changes.

### 7.3 Voice-first

Voice is a core interaction method, not an optional enhancement.

The user starts an active session once. During that session, PointPilot must hear follow-up turns and interruptions without requiring another click or hotkey.

### 7.4 Interruptible by default

User speech always has priority over PointPilot speech and pending computer actions.

### 7.5 Depth over breadth

The product should support fewer workflows with complete context, execution, verification, recovery, and safety rather than many disconnected commands.

### 7.6 Visible agency

PointPilot should visibly point, indicate its state, show what it is doing, and make consequential actions understandable.

### 7.7 Honest support boundaries

The architecture is general-purpose, but the first release must clearly identify GIMP as the only verified actuation environment.

## 8. Supported Environment

### 8.1 Verified platform

- Windows 11
- x64
- Local interactive desktop session
- One physical monitor
- 1920Ã—1080 display for the competition demo
- 100 percent display scaling for the competition demo
- English-language user interface
- Standard non-administrator session
- Keyboard, mouse, microphone, and speakers
- Network access to OpenAI APIs

### 8.2 Verified application environment

- GIMP 3.x
- Exact GIMP build pinned in `docs/demo-environment.md`
- English interface
- Single-window mode
- Fixed dock and panel arrangement documented with screenshots
- One provided layered demo project
- No unexpected modal dialogs at demo start

### 8.3 Best-effort behavior outside GIMP

PointPilot may support screen explanation and visual pointing in other foreground applications when the model can understand the screen.

Actuation outside the verified GIMP environment must be disabled by default in the hackathon build unless the user explicitly enables an unsafe development mode. The public demo must not rely on unsafe development mode.

## 9. Core User Experience

### 9.1 Session activation

The user starts PointPilot through one of:

- A global hotkey
- The tray icon
- The visible companion control

The default global hotkey should be configurable and must not use a reserved Windows shortcut.

After activation:

- The microphone remains active.
- A visible listening indicator remains on screen.
- The user can speak multiple turns without reactivation.
- The user can mute or stop listening at any time.
- Pressing Escape stops speech and active computer control immediately at the next safe action boundary.

PointPilot must not record or transmit microphone audio before the user starts an active session.

### 9.2 Teach mode

Example prompts:

- â€œWhat is this?â€
- â€œWhat does this panel do?â€
- â€œWhy canâ€™t I edit this?â€
- â€œWhat is selected right now?â€
- â€œWhy did that happen?â€

PointPilot must:

1. Capture the foreground window.
2. Use cursor position, screen context, and conversation context to resolve the reference.
3. Explain the concept briefly.
4. Point to or highlight the relevant interface element without moving the userâ€™s system cursor.
5. State uncertainty instead of inventing an answer when grounding is weak.
6. Offer one useful next step when appropriate.

Teach mode must not mutate the application.

### 9.3 Guide mode

Example prompts:

- â€œShow me how to add a shadow.â€
- â€œWhere do I change this?â€
- â€œWalk me through exporting this.â€
- â€œWhat should I do next?â€

PointPilot must:

1. Determine the current task goal.
2. Give one step at a time.
3. Point to the current target.
4. Wait for the user to perform the step.
5. Capture the updated screen.
6. Verify whether the expected change occurred.
7. Continue, correct, or recover.

The user must be able to say â€œdo it for meâ€ at any step. PointPilot must switch to Act mode while preserving the goal, completed steps, and constraints.

### 9.4 Act mode

Example prompts:

- â€œDo that for me.â€
- â€œFix this.â€
- â€œMake the shadow subtler.â€
- â€œMove this slightly left.â€
- â€œChange the subtitle and export it.â€

PointPilot must:

1. Restate or visibly summarize the intended outcome when the task has more than one mutation.
2. Capture the current foreground window.
3. Start a GPT-5.6 Computer Use loop.
4. Execute only supported mouse and keyboard actions.
5. Validate the active window before each mutating action.
6. Check cancellation before each action.
7. Capture updated screenshots during the task.
8. Verify the outcome before claiming success.
9. Stop when verification fails, the active application changes, or the user interrupts.
10. Provide a concise spoken and visual completion summary.

## 10. Voice and Interruption Requirements

### 10.1 Realtime voice

Use OpenAI Realtime speech-to-speech with the current recommended realtime voice model.

The implementation must support:

- Live microphone input
- Spoken model output
- Voice activity detection
- Natural turn taking
- Tool calls
- Barge-in
- Continuous multi-turn conversation during an active session

No separate speech-to-text or text-to-speech provider should be introduced for the first version.

### 10.2 Barge-in while speaking

When the user begins speaking while PointPilot is speaking:

1. PointPilot audio output must stop.
2. The interface must immediately switch to Listening.
3. The unfinished spoken response must not remain as completed conversation context.
4. The new user turn must be processed normally.

### 10.3 Barge-in while acting

When the user begins speaking while PointPilot is acting:

1. Set the current task cancellation token.
2. Increment the task revision.
3. Stop before the next atomic action.
4. Allow an action already sent to the operating system to complete if stopping it halfway would leave undefined input state.
5. Capture a fresh screenshot.
6. Transcribe and interpret the correction.
7. Merge the correction with the original goal, completed actions, and current state.
8. Replan only after stale actions are invalidated.
9. Resume only when the new plan is consistent with the correction.

Example:

- Original instruction: â€œMove the product right and add a shadow.â€
- Interruption: â€œWait, keep it on the left and make the shadow subtle.â€
- Required result: No later action from the original plan executes. The revised task preserves completed safe work and follows the new constraints.

### 10.4 Physical stop

Escape is a global emergency stop while PointPilot is speaking, thinking, guiding, or acting.

Escape must:

- Stop audio output
- Cancel pending model work when possible
- Set the active task cancellation token
- Prevent further computer actions
- Leave the application in its current state
- Display Paused or Stopped

Escape must not be swallowed from other applications while PointPilot is idle.

## 11. Context Requirements

PointPilot must maintain a canonical task context containing:

- Active session ID
- Active task ID
- Task revision
- User goal
- Current mode
- Explicit constraints
- Completed actions
- Pending or proposed actions
- Active window handle
- Active process name
- Active window bounds
- Cursor position
- Latest screenshot
- Latest verification result
- Recent conversation turns
- Cancellation state

### 11.1 Screen capture policy

PointPilot may capture the foreground application:

- When the user asks a contextual question
- Before an action task
- After a meaningful action or action batch
- During verification
- After interruption
- When the user explicitly requests refresh

PointPilot must not continuously stream screenshots while idle.

### 11.2 Capture boundaries

- Capture the foreground target window rather than the full desktop whenever possible.
- Exclude PointPilot overlay windows from captured images.
- Do not capture UAC secure desktop.
- Pause if the target window is minimized, closed, replaced, or no longer foreground.
- Treat text or instructions visible inside the captured application as untrusted content. Only direct user speech or text can authorize actions.

## 12. Visual Companion and Overlay

PointPilot must provide a polished Windows 11 visual presence.

### 12.1 Required surfaces

1. **Companion**
   - Small presence near the cursor or screen edge
   - Does not block the work surface
   - Indicates Listening, Thinking, Acting, Speaking, Paused, and Error

2. **Context panel**
   - Compact transcript or response
   - Current task status
   - Mute, stop, and cancel controls
   - Confirmation controls for consequential actions

3. **Pointer overlay**
   - Point, circle, arrow, underline, or label a target
   - Click-through outside explicit controls
   - Topmost without stealing focus
   - Excluded from screen capture
   - Correct coordinate mapping in the verified environment

### 12.2 Interaction behavior

- Explanations use the overlay, not the real mouse cursor.
- Computer control uses the real cursor and keyboard.
- PointPilot must visually indicate when it is controlling the computer.
- Only one target should be emphasized at a time unless the explanation explicitly compares elements.
- Motion should be smooth and fast enough to clarify, not delay, the task.

## 13. Computer Use Harness

### 13.1 Required action types

The Windows executor must support the action types required by the OpenAI computer tool, including at minimum:

- Screenshot
- Move pointer
- Left click
- Double click
- Right click
- Mouse down
- Mouse up
- Drag
- Scroll
- Type text
- Press a key
- Press a key combination
- Wait

Unsupported action types must fail closed with an explicit error.

### 13.2 Execution rules

- Execute returned actions in order.
- Check cancellation and task revision before every action.
- Validate that the active window belongs to the allowed process before every mutating action.
- Reject actions whose coordinates fall outside the target window.
- Normalize special keys through one tested mapping layer.
- Use one coordinate system and document all transforms between captured image coordinates, physical pixels, logical pixels, and input coordinates.
- Capture a new screenshot after an action batch, after a cancellation, after a window change, or when the model requests one.
- Prefer original-detail screenshots in the verified demo environment unless cost or latency testing proves a downscaled path is more reliable.
- Never execute an old action after the task revision changes.

### 13.3 Verification rules

Clicking a control is not proof of success.

PointPilot must verify through one or more of:

- Updated screenshot inspection
- Active window state
- Dialog presence or absence
- File existence after export
- Foreground process and title
- Expected visible text
- Expected selected layer or panel state when visually detectable
- A task-specific completion predicate

When verification is uncertain:

- Do not claim success.
- Pause and inspect again.
- Ask the user only when the ambiguity cannot be resolved safely.

## 14. Action Policy and Safety

### 14.1 Action levels

#### Observe

Examples:

- Screenshot
- Point
- Highlight
- Read visible content
- Wait

Behavior: Execute automatically.

#### Reversible edit

Examples:

- Select a layer
- Move an object
- Edit text
- Apply a non-destructive filter
- Toggle visibility
- Use normal GIMP edits retained in undo history

Behavior: Execute after the user requests Act mode. Record the action and preserve an undo path when practical.

#### Consequential action

Examples:

- Save
- Export
- Overwrite an existing file
- Close a document with unsaved changes
- Change a destination path

Behavior: Require explicit confirmation that identifies the outcome, target file, and whether existing data may be replaced.

#### Prohibited

Examples:

- Permanent deletion
- Entering credentials
- Interacting with password managers
- Payment or purchase
- UAC or security prompts
- Shell execution
- Disabling security controls
- Editing outside the allowlisted application
- Sending or publishing external content

Behavior: Refuse and stop.

### 14.2 Confirmation semantics

A voice instruction can authorize routine reversible edits that are directly necessary to the stated task.

A vague instruction such as â€œfix everythingâ€ cannot authorize consequential actions.

Confirmation must be tied to the exact action and current task revision. Confirmation from an earlier revision becomes invalid after interruption or replanning.

### 14.3 Undo

PointPilot must support â€œundo thatâ€ for the verified GIMP workflow.

The first version may use GIMPâ€™s native undo command and visual verification.

PointPilot must not promise undo for export, overwrite, or other external file operations.

## 15. GIMP Demo Vertical Slice

GIMP is the first verified actuation environment. It is not the product identity.

### 15.1 Demo fixture

The repository must contain or generate a layered demo project using original or properly licensed assets.

The fixture should contain clearly named layers such as:

- `Background`
- `Product`
- `Title`
- `Subtitle`

A reproducible script may generate an OpenRaster fixture if committing an XCF file is impractical.

The exact fixture setup must be documented.

### 15.2 Supported demo workflow

The required hero workflow is â€œrefine a promotional graphic.â€

The workflow must demonstrate:

1. **Contextual teaching**
   - User asks about the layers panel or selected layer.
   - PointPilot explains and points to the relevant interface area.

2. **Guidance**
   - User asks how to make the product stand out.
   - PointPilot points to the relevant tool or action search and gives one step.

3. **Mode switch**
   - User says â€œactually, do it for me.â€
   - PointPilot preserves the goal and enters Act mode.

4. **Computer control**
   - Select the intended layer.
   - Use GIMP action search or menus to apply a supported non-destructive effect.
   - Make a bounded layout or text change.
   - Verify the visible result.

5. **Interruption**
   - User changes a constraint while PointPilot is acting.
   - PointPilot stops the old plan, captures the new state, replans, and follows the correction.

6. **Completion**
   - Change the subtitle to a specified phrase.
   - Export a PNG after explicit confirmation.
   - Verify that the exported file exists.
   - Summarize what changed.

### 15.3 Required exact demo prompts

The demo environment must support prompts equivalent to:

1. â€œWhat is this panel and why are there multiple copies of my image?â€
2. â€œHow do I make the product stand out without changing the headline?â€
3. â€œActually, do it for me.â€
4. During execution: â€œWait, do not move it. Keep it on the left and make the shadow subtler.â€
5. â€œChange the subtitle to â€˜Built for Focusâ€™ and export it as a PNG.â€

The wording may vary, but the capabilities and state transitions must remain the same.

### 15.4 Supported GIMP techniques

The implementation may rely on:

- Visible menu navigation
- Keyboard shortcuts
- GIMP action search
- Layer selection
- Text editing
- Move and scale operations
- Non-destructive filters
- Export dialogs
- Native undo

The implementation must not rely on:

- A GIMP API
- A GIMP plug-in
- Hidden scripts that perform the edit outside the visible interface
- Pre-recorded action playback presented as live agency

## 16. Technical Architecture

### 16.1 Required stack

Preferred stack:

- C#
- .NET 10
- WPF
- Windows App SDK or Win32 interop where needed
- WebView2 for the Realtime WebRTC client
- TypeScript for the WebView2 Realtime surface
- OpenAI Realtime API
- OpenAI Responses API
- GPT-5.6 with the built-in computer tool

Do not introduce another desktop framework unless repository evidence shows the preferred stack cannot satisfy a required behavior.

### 16.2 Major components

#### PointPilot Desktop Shell

Responsibilities:

- Tray application
- Companion UI
- Context panel
- Overlay
- Global hotkey
- Escape stop
- Application state display
- Microphone permission flow

#### Realtime Voice Host

Responsibilities:

- WebRTC connection
- Microphone capture
- Spoken output
- Voice activity detection
- Barge-in
- Tool calls
- Multi-turn voice state

The Realtime host receives an ephemeral client secret. It must never receive the long-lived OpenAI API key.

#### Task Coordinator

Responsibilities:

- Canonical task state
- Mode transitions
- Task IDs and revisions
- Cancellation tokens
- Completed action journal
- Constraint updates
- Replanning after interruption
- One active computer task at a time

#### Context Capture Service

Responsibilities:

- Foreground window identification
- Process validation
- Window bounds
- Screenshot capture
- Overlay exclusion
- Screenshot encoding
- Coordinate mapping

#### Computer Use Service

Responsibilities:

- GPT-5.6 Responses calls
- Computer tool loop
- Returned action parsing
- Sequential execution
- Updated screenshot submission
- Final result extraction
- Failure handling

#### Windows Input Executor

Responsibilities:

- Mouse and keyboard input
- Key normalization
- Drag execution
- Cancellation checks
- Active-window validation
- Coordinate validation

#### Verification Service

Responsibilities:

- Post-action screenshot checks
- Task-specific completion rules
- Export file verification
- Success, uncertainty, or failure result

### 16.3 Data flow

```text
User speech
    |
    v
OpenAI Realtime session
    |
    | tool call
    v
Task Coordinator
    |
    +--> Teach/Guide request --> Context Capture --> GPT-5.6 vision result --> Overlay + voice
    |
    +--> Act request --> Context Capture --> GPT-5.6 Computer Use
                              |                    |
                              |<-- actions[] ------+
                              |
                              v
                       Windows Input Executor
                              |
                              v
                       Updated screenshot
                              |
                              v
                         Verification
                              |
                              v
                   Task Coordinator result
                              |
                              v
                   Realtime spoken response
```

### 16.4 Model responsibilities

#### Realtime model

- Listen and speak
- Maintain the live conversation
- Detect the userâ€™s requested mode
- Call local tools
- Provide short acknowledgements and final spoken summaries
- Never directly execute desktop actions

#### GPT-5.6 screen reasoning

- Understand screenshots
- Ground explanations
- Identify visual targets
- Produce structured teaching or guidance results

#### GPT-5.6 Computer Use

- Inspect the current UI
- Return computer actions
- Continue from updated screenshots
- Adapt to the current task goal and constraints
- Stop when the task is complete or unsafe

Only the Realtime model should produce user-facing speech. Computer Use model text is internal unless deliberately summarized by the Task Coordinator.

### 16.5 OpenAI credential handling

For the hackathon build:

- Read `OPENAI_API_KEY` from the developer environment or Windows Credential Manager.
- Do not hardcode it.
- Do not commit it.
- Do not expose it to WebView2 JavaScript.
- Use the .NET host to request an ephemeral Realtime client secret.
- Pass only the ephemeral secret to the WebView2 Realtime client.
- Make Responses API calls from the .NET host.
- Redact credentials from logs and exceptions.

A production remote token broker is outside the initial scope.

## 17. State Machine

The user-visible state machine must include:

- `Idle`
- `Connecting`
- `Listening`
- `Understanding`
- `Teaching`
- `Guiding`
- `Planning`
- `Acting`
- `Verifying`
- `Speaking`
- `Paused`
- `Error`

Required transitions include:

- Idle -> Connecting -> Listening
- Listening -> Understanding
- Understanding -> Teaching, Guiding, Planning, or Speaking
- Planning -> Acting
- Acting -> Verifying
- Verifying -> Acting, Speaking, Paused, or Error
- Speaking -> Listening
- Any active state -> Listening on voice interruption
- Any active state -> Paused on Escape
- Paused -> Listening on explicit resume
- Any state -> Error on unrecoverable integration failure
- Error -> Listening or Idle after recovery

Invalid transitions must be rejected and logged.

## 18. Reliability and Performance Requirements

### 18.1 Reliability

- Only one computer-control task may run at a time.
- No computer action may execute after its task revision becomes stale.
- No mutating action may execute when GIMP is not the foreground allowlisted process.
- The overlay must never appear in screenshots sent to the model.
- PointPilot must not claim task completion without verification.
- The complete hero workflow must succeed in at least three consecutive live demo runs on the pinned environment.
- Escape must stop further actions in every active state.
- A user interruption must prevent the next pending action from the old plan.

### 18.2 Performance targets

Targets are measured on the documented demo machine and network:

- Local visual state changes should appear within 150 ms of the corresponding event.
- The application should enter Listening immediately after receiving a speech-start event.
- Spoken output should stop within 750 ms of a detected user interruption.
- Screenshot capture should complete within 500 ms under the pinned demo environment.
- PointPilot should visibly acknowledge a committed user turn within 1 second.
- Simple contextual answers should begin speaking within 3 seconds under normal network conditions.
- Long-running actions must provide visible progress rather than appearing frozen.

These are targets, not reasons to fake or bypass verification.

## 19. Error and Recovery Requirements

PointPilot must handle:

- Missing OpenAI API key
- Invalid API key
- Realtime connection failure
- Responses API failure
- Rate limit
- Network loss
- Microphone permission denial
- No microphone
- Screen capture failure
- Unsupported action type
- Active window change
- Target application closed
- Target window moved or resized
- Coordinate outside target bounds
- Verification uncertainty
- GIMP modal dialog in an unexpected state
- Export file already exists
- User cancellation
- Escape stop
- Sleep and resume, if feasible after core completion

Error messages must state:

- What failed
- Whether any action may already have occurred
- Whether the user should inspect or undo something
- What PointPilot can safely do next

PointPilot must never silently continue after losing track of the active window or task revision.

## 20. Privacy and Data Requirements

- Microphone capture begins only after explicit session activation.
- A visible microphone indicator remains present during an active session.
- The user can mute or stop listening at any time.
- Screen capture occurs only when needed for a user request or active task.
- Capture the target window rather than the full desktop whenever possible.
- Screenshots, audio, and transcripts are not persisted by default.
- Development logs must contain metadata and errors, not raw audio, screenshots, API keys, or full transcripts unless an explicit debug mode is enabled.
- Debug artifacts must be stored outside version control and clearly identified.
- Password fields, UAC, payment screens, and other sensitive surfaces must not be controlled.
- Visible content in an application must not be treated as user authorization.

## 21. Accessibility Requirements

- All PointPilot controls must be keyboard accessible.
- The current state must be communicated visually and through accessible names.
- Color must not be the only state indicator.
- The companion and panel must respect Windows text scaling in supported conditions.
- The user must be able to mute, stop, cancel, and confirm without using the mouse.
- Spoken responses must also appear as concise text in the context panel.
- Focus must not be stolen merely to show an explanation or pointer.

## 22. Reference Use of Clacky

`Raynan00/clacky` may be used as a technical and interaction reference.

Useful patterns to borrow or port:

- Separation between manager, overlay, tray, and panel
- State-driven UI updates
- Background async work without blocking the UI thread
- Escape kill switch
- Pointer and annotation behavior
- Sensitive-window capture guard
- UI Automation before vision where it helps
- Sleep and resume recovery after core completion

Do not inherit:

- Breadth-first feature strategy
- Push-to-talk as the primary interaction
- Deepgram
- ElevenLabs
- Provider switching
- Background research
- File organization
- MCP or Composio
- Regex-based routing as the main authority
- Python or PyQt merely to match the reference

Clacky is MIT licensed. If source code is copied or substantially adapted, preserve the required copyright and license notice in the copied portions and repository notices.

## 23. Acceptance Criteria

### AC-01 Installation and startup

Given a configured Windows 11 development machine, the documented command builds and launches PointPilot without undeclared local dependencies.

### AC-02 Session activation

When the user starts a session, PointPilot enters Listening, shows a microphone indicator, and accepts multiple voice turns without another click or hotkey.

### AC-03 Realtime speech

PointPilot hears the user, responds with speech and visible text, and maintains conversational context across at least five turns.

### AC-04 Speech interruption

While PointPilot is speaking, user speech stops the current response and the new turn is handled without requiring a button.

### AC-05 Action interruption

While PointPilot is executing a multi-action GIMP task, a spoken correction invalidates the old task revision, prevents the next old-plan action, captures the current state, and replans.

### AC-06 Escape stop

Pressing Escape while PointPilot is active prevents further actions and stops speech.

### AC-07 Contextual teaching

With GIMP foreground and the user referring to a visible panel, PointPilot gives a grounded explanation and visually points to the correct area without moving the system cursor.

### AC-08 Guided workflow

PointPilot gives one GIMP step, waits for the user, captures the updated state, and correctly identifies whether the expected state changed.

### AC-09 Mode switching

During a guided workflow, â€œdo it for meâ€ starts computer control without losing the original goal or completed steps.

### AC-10 Computer control

PointPilot can complete the required GIMP hero workflow using visible mouse and keyboard actions produced through GPT-5.6 Computer Use.

### AC-11 Window safety

PointPilot refuses or pauses a mutating action if GIMP is not the foreground allowlisted process.

### AC-12 Coordinate safety

PointPilot rejects any action whose coordinates are outside the current target-window bounds.

### AC-13 Verification

PointPilot does not say a task succeeded until the final screenshot and task-specific checks support completion.

### AC-14 Export confirmation

PointPilot requests explicit confirmation before exporting and before overwriting an existing file.

### AC-15 Export verification

After export, PointPilot verifies that the expected PNG exists at the confirmed path.

### AC-16 Undo

After a supported reversible GIMP mutation, â€œundo thatâ€ invokes native undo and verifies a visible change.

### AC-17 Overlay exclusion

PointPilotâ€™s overlay does not appear in screenshots sent to GPT-5.6.

### AC-18 Failure handling

Missing credentials, microphone denial, API failure, screen capture failure, and unexpected window changes produce explicit safe error states without further actions.

### AC-19 Secret handling

No API key or live credential is committed, bundled in JavaScript, printed in logs, or included in screenshots.

### AC-20 Repeatability

The documented hero workflow completes successfully in three consecutive runs on the pinned demo environment.

## 24. Testing Requirements

### 24.1 Unit tests

At minimum:

- State-machine transitions
- Task revision invalidation
- Cancellation behavior
- Action policy classification
- Active-window validation
- Coordinate mapping
- Coordinate bounds
- Key normalization
- Confirmation invalidation after interruption
- Secret redaction
- Error-state mapping

### 24.2 Integration tests

Use fakes where live APIs are not required:

- Fake Realtime events
- Fake Computer Use responses
- Fake action executor
- Fake screenshot service
- Fake verification result

Test:

- Teach flow
- Guide flow
- Act flow
- Speech interruption
- Action interruption
- Escape stop
- Stale action rejection
- Active-window change
- Verification failure
- API error recovery

Mocks must be clearly identified and must not be presented as live verification.

### 24.3 Live tests

When credentials and GIMP are available:

- Realtime voice connection
- Multi-turn speech
- Barge-in
- Contextual screen explanation
- One-action computer control
- Multi-action computer control
- Mid-task correction
- Escape stop
- Export confirmation and verification
- Three complete hero workflow runs

### 24.4 Build checks

The repository must document and run, when applicable:

- Dependency restore
- Build
- Unit tests
- Integration tests
- Formatting or linting
- Type checking for TypeScript
- Secret scan
- Packaging or runnable release build

## 25. Demo Script Requirements

The final demo should be one continuous story, not a feature montage.

Suggested structure:

### 0:00 to 0:25 - Problem

Explain the context-transfer problem:

- Leave the app
- Open a chatbot
- Upload or paste context
- Translate the answer back into clicks

### 0:25 to 0:55 - Teach

Inside GIMP:

- Ask what the layers panel means.
- PointPilot explains and points inside the live interface.

### 0:55 to 1:20 - Guide

Ask how to make the product stand out without changing the headline.

- PointPilot gives one contextual step.
- The user begins following it.

### 1:20 to 1:50 - Act

Say â€œactually, do it for me.â€

- PointPilot takes over visibly.
- It selects the correct layer and starts the edit.

### 1:50 to 2:15 - Interrupt

Interrupt with a changed constraint.

- PointPilot stops the old plan.
- It acknowledges the correction.
- It replans from the current screen.

### 2:15 to 2:40 - Complete and verify

- Change the subtitle.
- Confirm PNG export.
- Verify the output file.
- Show the final graphic.

### 2:40 to 3:00 - Technical close

State:

- OpenAI Realtime provides natural voice and barge-in.
- GPT-5.6 Computer Use drives the live interface.
- PointPilot provides the Windows harness, cancellation, visual overlay, safety, and verification.
- Codex was used to build and test the implementation.

## 26. Key Risks and Mitigations

### Risk 1: Hidden GIMP state causes incorrect actions

Mitigation:

- Pin the demo environment.
- Capture before and after actions.
- Validate foreground process.
- Prefer keyboard shortcuts and action search.
- Stop on uncertainty.
- Keep the workflow narrow.

### Risk 2: Voice and computer loops diverge

Mitigation:

- One canonical Task Coordinator.
- One active task.
- Task IDs and revisions.
- Realtime model does not directly execute.
- Computer Use model does not speak directly to the user.

### Risk 3: User interruption arrives during an action

Mitigation:

- Treat each input action as an atomic boundary.
- Set cancellation immediately.
- Stop before the next action.
- Capture and replan.

### Risk 4: Coordinate drift

Mitigation:

- Fixed verified resolution and scaling.
- Active-window bounds.
- One documented coordinate transform.
- Bounds checks.
- Overlay exclusion.
- Original-detail screenshots initially.

### Risk 5: Broad product claims exceed implementation

Mitigation:

- Present PointPilot as general-purpose by interaction model and architecture.
- State clearly that GIMP is the first verified actuation environment.
- Do not claim universal reliability.

### Risk 6: Model follows instructions embedded in the screen

Mitigation:

- Treat all visible application content as untrusted.
- Only direct user input grants permission.
- Restrict actuation to the foreground allowlisted process.
- Require confirmation for consequential actions.

### Risk 7: Latency makes the product feel unresponsive

Mitigation:

- Immediate local state updates.
- Short spoken acknowledgements.
- Visible acting and verifying indicators.
- Avoid unnecessary model calls.
- Keep screenshot size fixed and tested.

## 27. Required Repository Deliverables

The completed repository should include:

- Working PointPilot desktop application
- Realtime voice integration
- GPT-5.6 Computer Use integration
- Windows capture and input harness
- Companion, context panel, and pointer overlay
- Cancellation and task-revision system
- Safety and confirmation policy
- Verification service
- Demo fixture generator or committed fixture
- Automated tests
- `README.md`
- `docs/product-scope.md`
- `docs/architecture.md`
- `docs/demo-environment.md`
- `docs/demo-script.md`
- `docs/security-and-privacy.md`
- `.env.example` with names and placeholders only
- License notices for any adapted third-party code
- Concise permanent `AGENTS.md`
- Evidence distinguishing new Build Week work from any pre-existing work

## 28. Definition of Done

PointPilot is complete for this PRD only when:

1. The application is runnable on the pinned Windows 11 environment.
2. The microphone session is continuous after one activation.
3. The user can interrupt speech and computer control naturally.
4. Teach mode points and explains in the live GIMP interface.
5. Guide mode advances one verified step at a time.
6. Act mode completes the required visible GIMP workflow through GPT-5.6 Computer Use.
7. A spoken correction changes the active task without old-plan actions continuing.
8. Escape stops further actions.
9. Consequential file actions require confirmation.
10. The final result is verified.
11. The hero workflow passes three consecutive live runs.
12. Tests and build checks pass.
13. No secrets are committed.
14. Required documentation is complete.
15. The project does not misrepresent mocks or unsupported applications as verified.
16. The companion bootstrap process reaches `PASS` or `PASS WITH RESIDUAL RISK`.

## 29. Locked Product Decisions

The following decisions are final for the initial implementation unless repository evidence proves a required behavior is impossible:

- PointPilot is a general-purpose co-present desktop AI, not a GIMP-specific product.
- GIMP is the first verified actuation and demo environment.
- Windows 11 is the only supported operating system.
- Voice is mandatory.
- The microphone remains active throughout an explicitly started session.
- No button is required between turns.
- Natural voice interruption is mandatory.
- Escape is the physical emergency stop.
- GPT-5.6 Computer Use is the primary actuation planner.
- PointPilot executes actions locally through a Windows harness.
- No GIMP API or plug-in is used.
- No Google Slides API or other application API is used.
- No ElevenLabs or Deepgram integration is used.
- Depth and repeatability take priority over additional features.
- The system must verify before claiming success.
- The public demo must be honest about the verified application boundary.

