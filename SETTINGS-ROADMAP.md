# RadialMenu Settings — Roadmap & Architecture

This document describes a comprehensive, production-ready plan to redesign the Settings experience for RadialMenu. The goal is to provide a clean, accessible, and delightfully usable configuration experience that supports power users and beginners alike.

This roadmap covers: high-level goals, UX principles, information architecture, data model and migrations, UI component and interaction design (including a drag-and-drop Menu Builder), validation and safety, testing, rollout plan, and an actionable implementation checklist.

---

**High-level Goals**
- Provide a single, consistent Settings surface that is discoverable from the system tray.
- Make common tasks (set hotkey, edit menu items, change appearance) doable in 1–3 clicks.
- Support live preview so changes are reversible and exploratory.
- Ensure robust validation, migration, and safe handling of actions/paths.
- Prioritize accessibility, keyboard-first usage, and low cognitive load.

**Design Principles**
- Progressive Disclosure: surface simple options first, advanced options behind a clear toggle.
- Immediate Feedback: show real-time changes in a lightweight preview canvas.
- Safety First: changes are only committed on explicit Save; autosave + snapshots available.
- Familiar Patterns: use left navigation with clear categories, search, and keyboard shortcuts.
- Performance: keep load/save fast even for large menus; lazy-load heavy UI (drag builder, preview).

---

**Information Architecture (Top-level sections)**
- General
  - Startup behavior (run on login), tray icon, language, backups
  - App theme (light/dark/auto), UI scale
- Hotkeys
  - Global hotkey (capture control), alternative activation, conflict detection
- Appearance
  - Inner/outer radius, colors, fonts, center text, animations
  - Presets and import/export of appearance
- Menu Builder (Visual)
  - Drag-and-drop canvas + tree view
  - Add/Edit/Delete items, reorder, nest, set icons/colors/actions
  - Bulk operations (duplicate, clone subtree, import JSON)
  - Live preview and test activation button
- Advanced
  - JSON editor with schema validation and diff preview
  - Import/Export profiles, migrations, backup management
- Diagnostics
  - Logging, config validation results, telemetry opt-in (opt-in only)

---

**Data Model & Config Schema (versioned)**

Use a top-level `version` integer in the JSON config. Example schema (high level):

{
  "version": 2,
  "meta": { "profileName": "Default", "lastModified": "..." },
  "hotkeys": { "toggle": "Win+F12" },
  "appearance": { "uiScale": 1.0, "innerRadius": 40, "outerRadius": 220, "theme": "dark", "centerText": "MENU", "colors": { ... } },
  "menu": [ { "id": "uuid", "label": "Open", "icon": "📄", "color": "#FF5A5A", "action": "launch", "path": "C:\\\\...", "submenu": [...] } ]
}

Schema considerations
- Use explicit `id` for items (UUID) to support stable references.
- Separate `action` and `path` and validate but never auto-execute on load.
- Provide `metadata` to support ordering, collapsed state, and custom fields.
- Allow `profiles` array for saved configurations.

Migration rules
- Provide a `SettingsService` that performs migrations by version. Always backup original file before migrating.

---

**SettingsService responsibilities**
- Load and validate config file; fallback to default if missing/invalid.
- Save config atomically (write temp file + replace); keep timestamped backups.
- Migrate older versions using stepwise migration handlers.
- Expose an API for runtime to subscribe to config changes and to request previews.
- Provide undo/redo snapshots for SettingsWindow editing session (not global save history).

---

**SettingsWindow UX & Interaction Patterns**

Layout
- Two-pane layout: left vertical navigation (icons + labels), right content area.
- Top toolbar with `Search`, `Save`, `Cancel`, `Undo`, `Redo`, `Import`, `Export`.
- Right-most collapsible live preview panel.

Primary interactions
- Save/Cancel: explicit Save commits to disk and reloads runtime via event; Cancel discards unsaved changes.
- Live Preview: applying changes to preview (not to runtime) — a separate 'Apply' button for applying to running app without closing settings.
- Inline help: small info icons with short descriptions + link to docs.

Menu Builder specifics
- Dual representation: tree (left) + canvas (center). Selecting an item highlights on both.
- Drag within tree to reorder and nest; drag from a palette to add new items.
- Drag from palette -> position computes angle and radius, or user can edit numeric properties.
- Context menu on item: Add child, Duplicate, Delete, Edit properties.
- Properties panel (right) shows editable fields: Label, Icon (emoji or glyph), Color (picker), Action Type (None/Open/RunCommand), Path, Hotkey override, Conditional visibility.
- Bulk select to apply color or action to multiple items.
- Inline validation for path existence and action type.

Hotkey editor
- Capture control that records modifiers and key; shows human-friendly string.
- Detect conflicts with existing hotkeys; warn inline and show conflicting components.

Advanced JSON editor
- Use a code editor control with JSON Schema validation and linting (errors show inline).
- 'Preview diff' before applying; 'Validate & Save' button.

Accessibility
- Full keyboard navigation, focus outlines, ARIA/AutomationProperties on controls.
- High-contrast friendly color defaults, scalable fonts, and screen-reader text for icons.

---

**Implementation Plan and Priorities**

Phase 1 — Foundation (MVP)
- Create `SettingsService` and `Settings` model with schema v1 and safe load/save.
- Implement new `SettingsWindow` skeleton with left nav (General/Hotkeys/Appearance/Menu/Advanced) and Save/Cancel.
- Add hotkey capture control and conflict detection.
- Add appearance page with basic live preview toggles.

Phase 2 — Menu Builder & UX
- Build tree + canvas Menu Builder with drag-and-drop reorder and property editor.
- Add live preview panel that reflects changes locally.
- Implement Import/Export and JSON editor (read-only initially).

Phase 3 — Advanced features
- Undo/Redo, snapshots, migration support, backups.
- Validation, warnings, and diagnostics page.
- Accessibility polish and keyboard-first flows.

Phase 4 — Tests & polish
- Unit tests for SettingsService, UI tests (automation) for common flows, performance tuning.

---

**Detailed Task Checklist (developer-oriented)**

1. SettingsService
   - Create `Models/Settings.cs` (versioned models) and default values.
   - Implement `Services/SettingsService.cs` with Load/Save/Migrate/Backup.
   - Add unit tests for serialization and migrations.

2. Settings UI skeleton
   - Add `Views/SettingsWindow.xaml` and `ViewModels/SettingsViewModel.cs`.
   - Implement left nav control and page host.
   - Hook Save/Cancel to `SettingsService`.

3. Hotkey editor
   - Create `Controls/HotkeyCaptureControl.xaml` with capture and visual display.
   - Integrate into Hotkeys page and validate.

4. Appearance page
   - Controls for radii, colors, UI scale, theme, center text.
   - Add color picker and presets.

5. Menu Builder
   - Create `Controls/TreeMenuEditor` and `Controls/CanvasMenuPreview`.
   - Implement drag/drop with reorder and nesting using `ObservableCollection` and commands.
   - Properties panel for editing item details and validation.

6. JSON Editor & Import/Export
   - Integrate a lightweight JSON editor control; add schema validation using `Newtonsoft.Json.Schema` or runtime validation.
   - Implement import/export flow and safe apply.

7. Undo/Redo and Snapshots
   - Add transaction helper to SettingsService or ViewModel to snapshot state and apply undo/redo.

8. Diagnostics & Backups
   - Implement backups folder with timestamped copies and UI to restore.

9. Testing & CI
   - Unit tests, UI automation tests, and add results to build pipeline.

10. Docs & Release
   - Update `README.md` and `QUICK-START.md` with settings usage and migration notes.

---

**UX Micro-interactions & Visual Details**
- Subtle animations when adding/removing items (fade + scale).
- Inline validation messages appear under inputs with concise remediation steps.
- 'Test' button in Menu Builder triggers a simulated popup near the preview region.
- Color contrast checker displays a small indicator if low contrast detected.

---

**Safety & Security**
- Never auto-run `Action` or `Path` on load. Provide explicit test/run buttons.
- Validate file paths for existence but allow user override.
- For commands requiring admin rights, show a warning and an option to open an elevated runner.

---

**Open Questions / Decisions to Make**
- Do we want to embed a JS-based JSON editor (monaco) or keep everything in WPF controls? (Monaco gives rich editing but increases bundle complexity.)
- How aggressive should autosave be? (I recommend explicit Save by default, with optional autosave toggle.)
- Do we need profile syncing across machines? (If yes, we need integration for cloud or manual import/export.)

---

If you want, I can start implementing Phase 1 now: add `SettingsService`, models, and the skeleton `SettingsWindow` with MVVM wiring and Save/Cancel behavior. Tell me if you prefer a particular folder structure or to use a specific JSON schema library.

---
© RadialMenu — Settings Roadmap
