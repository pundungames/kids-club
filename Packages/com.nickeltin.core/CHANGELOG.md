## [1.2.9]- Moved project settings api to separate package

## [1.2.8] - Cumulative updates
- Finalized for release

## [1.2.7] - Internals upd
- Bunch of internal bridge upds
- Context menu organization
- LazyLoadReference fix

## [1.2.6] - Project management upd
- Removed update functionality (auto update checks, updates from either registry or git repo)
- Removed packages defines (module definitions), now packages does not contribute to project scripting symbols

## [1.2.4] - Search windows api upd
- Fixed search window api, editor window were stuck upon creation 
- Moved create window to direct Assets context menu
- Added project settings toggle to show create window, disabled by default
- Added GenericSearchWindow
### Other
- Changed display name
### Plans for future
- Move search window api to new unity system, either AdvancedDropdown or Searcher package in unity 2022

## [1.2.0] - Project settings
- Added project settings with:
  - Check for updates toggle
  - Ignored versions list

## [1.2.0] - Unity internal bridge
- Added dependency to com.unity.ugui 1.0.0 and created internal bridge assembly to access unity internals
  - Created bunch of classes started with "_" that indicates that its exposed unity internal
  - Added classes like _SerializedProperty, _ProjectWindowUtil, _AssetPreview, _AssetImporter, _EditorGUI
- Added EditorSerialization.cs as wrapper for InternalEditorUtility.LoadSerializedFileAndForget()

## [1.1.19] - Different fixes
- Fixed asset cacher errors (cause by late initialization, now init is in static constructor)
- Fixed serialized property extension (GUI context menu click error)

## [1.1.18] - Added editor method for ui context creation 

## [1.1.16] - Menu paths upd

## [1.1.15] - Cumulative Update
- fixed IMGUIEventsCaptureWindow used in search windows
- Added StaticPlayerLoop - adds static callbacks before update, after update, and similar for fixed update
- Added ToolbarExtensions - call ToolbarExtensions.RegisterExtension() to add custom button/toggle/dropdown/etc to main toolbar

## [1.1.14] - Cumulative Update
- Core updater, supports Git and registry remotes. Will popup to offer version update.
- Fixed some 2020 incompatible parts
- Added workflow to publish in public registry
- Fully remade SerializedPropertyExt.cs, now all property Get/Set supports nesting, lists, arrays, serialized references and multi-editing.
For type getting built-in internal unity methods from ScriptAttributeUtility was used.
- GUIDField is now a class, and has hashing optimizations

## [1.0.0] - First release
- Fixed/removed external dependencies
- Removed CodeGeneration~ ignored folder (was causing warnings)
- Removed EmbeddedPackages
- Removed CoreModule def
- Better package/modules define support (now package can update defines even after deletion)

## [0.0.1] - Init
- Core package were created