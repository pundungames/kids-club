## [1.3.2] - Removed old api dependencies
- Removed project settings api that was not used, but was causing problems with build under certian conditions

## [1.3.1] - Bugfixes
- Fixed when upon duplication SDF Image Parent object in editor, FirstLayerRenderer got stuck with 0 scale
- Added hide flags dropdown for better debugging of FLR

## [1.3.0] - Runtime SDF Generation Support and CPU Jobs backend
- SDF now can be generated in runtime, not only in editor as it previously was
- With this update also coming new WebGL demo
### CPU SDF Generation Backend
- In addition to legacy GPU backend there is now new CPU jobs based backend.
- In addition default option for backend is "auto", which will choose GPU prefferebly first, if machine have acces to GPU, and fallback to CPU elsewise.
- CPU backend now supports


## [1.2.2] - Cumulative updates
- Changed context menus paths
- Added welcome window
- Removed obsolete "Modules" API, and automatic project defines additions
- Added support for in-scene SDFImage visibility toggle (previously was not working)

## [1.2.1] - SDFSpriteReferenceList
- Added class to mass-reference sdf's, it handles all DragAndDrop scenarios in editor.
- Some internal changes, more utils functions, context menus, small change to import code.

## [1.2.0] - BIG UPDATE
- Decoupled pipeline is now in release state. 
- Removed SDFAtlases since with Decoupled pipeline its possible to setup
everything in regular atlases.
- Removed SDFSpriteModule. Now all SDF Import Settings is drawn in TextureImporter.
- Native Presets support. Removed all old preset handling code.
- Addressables/Resources support. Decoupled pipeline makes all assets visible 
allowing to load them trough thees systems.
- Better Drag&Drop support for sprites/textures/sdfSprites/sdfMetaAssets. 
All of them now support dragging into sdf image field, generating sdf if none found,
or searching for decoupled equivalents.
- Simplified `struct SDFSpriteReference`, removed most of 2023 metadata pipeline differences.
Now all serialization differences between Unity 2023 and older is removed, 
Unity 2023 now just adds optional ability to get sdf products from sprite,
while overall pipeline is now consistent between versions and using the same data.
### Samples reword
- Basic samples rework
- New scripting samples, covering regular and decoupled pipeline, addressables support and general scripting.

## [1.1.14] - Better UX
- SDF settings now drawn as part of texture importer inspector, under sprite section. 
If custom editor failed to load them will be fully drawn in header.
- Removed sprite module. Since all sdf settings is now exposed, no need to have separate window.
Previews is not so helpful either way, easier way is to observe changes in SDF Image on scene.
- Removed SDFAtlas. Decoupled pipeline introduced better way to use built-in atlases.

## [1.1.13] - Decoupled pipeline
Now import pipeline in addition to existing one. Feature to load sdf sprites with Resources or with Addressables 
was requested by many users, but current pipeline before 2023 don't allow that due to SDFSpriteMetadataAsset being hidden.
This decoupled pipeline allows to create *.sdfasset file separate from original texture, with own sdf import settings
- SDFAsset new type of asset with *.sdfasset extension that holds reference to existing texture
- Exposed SDFSpriteMetadataAsset to public
- Reworked SDF import steps allowing to create overrides for SDFAsset
- Updated search to display meta assets from SDFAsset

## [1.1.12] - Presets & Fixes
- Added presets gui support, now in preset file sdf settings will be drawn on top of all other texture importer settings
- Changed distribution structure, it does not affect the package itself but affects content bundle in form of *.unitypackage in asset store.
- Fixed FLR disappearance bug when disabling SDF Image
- Changed package display name
- SDF Metadata asset UX. Added filed in sprite header to quickly select asset. Now for sprite preview border is drawn.
- Fixed SDF sprite pivot and border issue, they were generated wrong with some import settings. 
Now pivot is adjusted to be the same position as in source sprite but with border offset.
Sprite border (native in unity used for sliced sprites) is now 

## [1.1.7] - Fixes & Other
- Pure shader force include in build
- Fixed multi-edit properties value override for layers Toggle
- Changed FLR lifecycle, now it's reused and fetched from hierarchy
- Moved search to VirtualArtifacts files instead of nested TextAssets for search index
- Fixed SDFSpriteModule texture flickering when preview mode changes
- Returned SDFImage <-> Image conversion (even tho all layers data will be gone)

## [1.1.6] - Fixes & Samples upd
- Extended Basics/TextureImport docs
- Added Pure SDF Image sample

## [1.1.5] - Fixes
- Fixed first layer wrong Z in camera-space canvases
- Fixed SDF import settings validation (was causing import errors)

## [1.1.4] - Pure SDF Image
- Added Pure SDF Image - component that renders only sdf without original texture and
allows to blend between two sdf's
- Pure SDF UI Shader
- Created some base classes for SDFGraphic and Editor
- PureSDFImageRenderingStack - allows to control multiple Pure images 
- Fixed SDF Importer TextureProcessed event, which was not working correctly in parallel import mode. 
Now its writing serialized events callbacks to *.json file in Temp folder and then on main editor thread extracting data from it.
Weirdly this is the only way I achieved this working, unity creates some kind of isolated runtime for Thread Worker for parallel import, apparently.

## [1.1.1] - First layer renderer fix 
- First layer renderer is hidden component that renders regular part of SDFImage, it was causing errors. Now their fixed.

## [1.1.0] - BIG UPDATE 
- 🔥Fixed Mac M1 Metal renderer sdf generation artifact🎉
### SDFImage
- Now inherits from `MaskableGraphic` instead of `Image`
- All base `Image` properties and logic is ported
- Improved editor, now for better UX
- Added much more per instance properties, they are structured as layers
  - `Main Color` - overall color tint for all layers
  - Regular layer - this is where regular sprite is rendered
    - `Enabled` - is mesh for this layer generated
    - `Color` - color of this layer
  - Outline layer - first layer of SDF material
    - `Enabled` - is mesh for this layer generated
    - `Color` - color of this layer
    - `Width` - width of SDF effect, (previously was only in material)
  - Shadow layer - second sdf layer
    - `Enabled` - is mesh for this layer generated
    - `Color` - color of this layer
    - `Width` - width of SDF effect, (previously was only in material)
    - `Offset` - shadow is generated on separate mesh, and this mesh can be offseted. Offset is now implemented on mesh level rather then on shader layer, allowing to provide quality non-distorted shadow for any image modes.
- Added preview for both sdf and regular sprite
- Material rework
  - SDF Display material was completely re-written to support new per-instance approach 
  - Better blending between layers, no dark edges
  - Better anti-aliasing (but i believe better implementation is possible)
- Better batching support
  - Now for regular sprite hidden game object with `CanvasRenderer` are generated, allowing to render regular sprite with its texture while still supporting batching and alpha textures.
  - For sdf sprite `SDFImage` `CanvasRenderer` is used, but its mesh now consist of two parts, `Outline` and `Shadow` allowing to properly offset shadow in any image mode
  - Single material used for both renderers, this allows for batching to work properly, even tho using two different main textures
  - Since `SDFImage` now support atlases batching can go even further

### Big import pipeline rework
- Completely removed Secondary texture workflow
- Now sdf sprites are generated alongside with regular sprites
- Removed plain textures support, only sprite textures are supported now
- Improved multi-sprite texture, now now matter how sprites are layouted in texture they will work properly
  - Sprites now packed to atlas texture with border offset for each sprite
- Border offset changed to be pixel value, rather then scale
- Generated SDF texture is now `Alpha8 UNorm`, containing only one channel it has smaller size(overall texture size in increased due to inefficiency packing of `Texture2D.PackTextures()`)
- Along with sprites generated hidden assets called `SDFMetadataAsset` (for each sprite)it stores reference to both SDF and regular sprite, and their final metadata data
- Import pipeline made faster with use of `RenderTexture` along all import process, now each sdf sprite generated in separate draw call, so overall speed might be slower(in future need to replace `Texture2D.PackTextures()` to work with render textures, and to pack textures more space-efficiently)
- `Gradient Size` and `Border Offset` import settings is now unified between all texture sizes
  - Gradient size will work the same for any texture size, resulting into same final sdf effect width for any sprites
  - Border offset is in pixels, but will scale to account to texture compression
- Improved image modes
- Fixed parallel import bugs
- For sdf sprite reference now `struct SDFSprite` is used

### Sprite module (new SDF Importer Window)
- Removed legacy `SDF Importer Window`, now all sdf import setup can be found in `Sprite Editor` - built-in unity editor window
- `SDF Importer` is now one of the section of sprite editor along with `Secondary textures` and `Custom Physics Shape`
- Features Apply/Revert functionally, and preview (tho most of previous preview were removed dut to its no yielding enough quality, might be added in future)
- Unlike all other default sprite modules support multi-editing

### Atlases support
- Atlases is finally supported!
- Since Regular sprites and SDF sprites are two separate entities now they can be packed to atlases
- There is some rules on how to pack them (even the they can be pack natively as any other texture there might be sdf distortion due to default atlas texture compression)
  - Regular sprites and sdf sprites need to be packed to two different atlases
    - Regular to any atlas without tight packing and rotation
    - SDF to atlas with without tight packing and rotation AND texture with `Alpha8` format. It has no compression, that's important
  - You need specify each sprite by hand to be packed in particular atlas, or make some asset postprocessor
  - The general solution provided to automate this process is called `SDF Sprite Atlas`
    - This is asset much like `Sprite Atlas` where you can specify texture or sprites (unfortunately no folders support)
    - It holds reference to regular `Sprite Atlas` and sdf `Sprite Atlas`
    - On reimport it will fill both atlases with correct sprites, you can modify other `Sprite Atlas` properties as you like
    - ⚠️`SDF Sprite Atlas` only works in for `Sprite Atlas V2` mode 

### Pixel art support
- Now sdf image support pixel art
- How to make it work
  - Import texture in `Point (no filter)` mode
  - Set in SDF import settings `Resolution Scale` to 1. In pixel art each pixel is important
  - In SDF Material set `Distance Softness` to 0, so there was no smoothing for pixels

### 2023 Version Support (New Metadata Pipeline)
- Unity 2023 version has introduced extended sprite api: `Sprite.AddScriptableObject()`, `Sprite.GetScriptableObjects()`, etc.
It allows to associate import data with sprite, which is exactly what SDF sprite needed.
- To make sdf sprite work with different versions a lot of branching and abstraction layers were made
  - `struct SDFSprite` - use it to reference sprite, in version 2023 references sprite itself, 
  for versions before references `SDFMetadataAsset`. Creates serialization surrogate to keep all required
  references for both versions
  - `SDFEditorUtil` - unifies all version pipelines, since you can get `SDFMetadataAsset` in any editor version
  - `SDFUtil.2023Support` - runtime utilities for new api
- In version 2023 and higher the main differences is
  - You can set sprite directly in sdf image in code property `Sprite` has setter.
  In older version you can only set wrapper `struct SDFSprite`
  - In editor you browsing for sdf sprite with default search engine
  In older version used custom search provider to browse sdf artifacts
- For package itself define `SDF_NEW_SPRITE_METADATA` is used, if you want to know is new metadata pipeline used externally use `SDFUtil.IsNewSpriteMetadataEnabled`


### SDF Artifacts Search
- Creates custom search provider based upon QuickSearch package to browse SDF import artifacts since they are hidden and regular ProjectWindow can't display them.
- To use light-weight search all sdf artifacts is indexed in text files generated for sdf textures. This allows to don't load assets in search but rather use GlobalObjectId (GUID and LocalFileID pair) to get its preview, and then on demand - reference
- Search provider with id "sdfa:" is particularly useful for version of package before Unity 2023 where sprite api was extended.

### Other
  - Added bridge to unity internals (2d.sprite), removing reflection calls. Overall using internals extensively.
  - Updated editor icons
  - Deleted core bridge dll, now distributed with `com.nickeltin.core-bridge` package bundled 

    
## [1.0.18] - Advanced materials
- Ramp textures is supported now for Main and Outline layer of material
- Overlay textures in Main and Outline layers, they can be animated (scrolling) There are problems with that, because of local UV is unknown

## [1.0.17] - Pixel art support
- now sdf textures inherit filter mode of original textures allowing to render pixel-prefect outlines
- sdf importer improvements, pixel art preview fix, locking functionality

## [1.0.16] - Minor fixes
- updated README.md
- fixed sprite preview tab in sdf importer

## [1.0.15] - Better creation context menu UX

## [1.0.14] - Shader WebGL keywords fix

## [1.0.13] - ToolbarPath fix

## [1.0.12] - Cumulative update
- Completed core bridge
- New samples
- Removed obsolete backends
- Readme update
- SDF project defines
- Per-instance color and width (SDF multiplier) support
- SDFPreview, new system to render SDFImage in preview scene, used in SDFImporter
- All image modes support Simple, Tiled, Filled (with some nuances), Sliced

## [0.0.1] - Init
- Core package were created