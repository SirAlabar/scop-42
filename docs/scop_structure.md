# SCOP — Complete Project Structure

**Language:** C# (.NET 8)  
**Platform:** Linux (WSL Ubuntu + Ubuntu)  
**Rendering:** OpenGL 4.1 Core via OpenTK 4  

---

## Full Directory Tree

```
scop/
│
├── Makefile                          ← all / clean / fclean / re
├── scop.csproj                       ← .NET 8 project + OpenTK NuGet ref
├── README.md                         ← build instructions, controls
│
├── src/
│   │
│   ├── Math/
│   │   ├── Vec2.cs                   ← 2D float vector (UV coords)
│   │   ├── Vec3.cs                   ← 3D float vector (positions, colors)
│   │   ├── Vec4.cs                   ← 4D float vector (matrix multiply)
│   │   └── Mat4.cs                   ← 4×4 column-major matrix (all transforms)
│   │
│   ├── Parsing/
│   │   ├── Interfaces/
│   │   │   ├── ITriangulator.cs      ← Strategy: contract for triangulation algorithms
│   │   │   └── IUvMapper.cs          ← Strategy: contract for UV mapping algorithms
│   │   ├── Triangulation/
│   │   │   ├── FanTriangulator.cs    ← Strategy: fan method (mandatory)
│   │   │   └── EarClipper.cs         ← Strategy: ear-clipping (bonus 1)
│   │   ├── UvMapping/
│   │   │   ├── BoxUvMapper.cs        ← Strategy: XY plane projection (mandatory)
│   │   │   └── FaceNormalUvMapper.cs ← Strategy: per-face normal projection (bonus 2)
│   │   ├── Vertex.cs                 ← interleaved GPU vertex struct
│   │   └── ObjParser.cs              ← .obj parser — uses ITriangulator + IUvMapper
│   │
│   ├── Utils/
│   │   ├── FileValidator.cs          ← shared file existence + permission check
│   │   └── FileParser.cs             ← shared float parsing, line tokenizer, PPM token reader
│   │
│   ├── Rendering/
│   │   ├── Interfaces/
│   │   │   └── ITexture.cs           ← Null Object: contract for Texture + NullTexture
│   │   ├── Shader.cs                 ← GLSL load / compile / link / uniforms
│   │   ├── Mesh.cs                   ← VAO / VBO / EBO + Draw()
│   │   ├── Texture.cs                ← Factory + real impl: loads BMP/PPM, uploads to GPU
│   │   └── NullTexture.cs            ← Null Object: silent no-op, used when no texture loaded
│   │
│   ├── App.cs                        ← GameWindow subclass, thin orchestrator
│   ├── AppState.cs                   ← all runtime state in one place
│   ├── InputHandler.cs               ← keyboard + mouse input, mutates AppState
│   ├── Renderer.cs                   ← builds MVP matrices, sets uniforms, draws
│   └── Program.cs                    ← entry point, arg parsing, usage print
│
├── shaders/
│   ├── vertex.glsl                   ← MVP transform, pass color + UV
│   └── fragment.glsl                 ← mix(faceColor, texture, blendFactor)
│
└── assets/
    ├── models/
    │   ├── 42logo.obj                ← provided by subject (mandatory defense)
    │   ├── teapot_original.obj       ← provided by subject (bonus 1 test)
    │   ├── teapot_blender.obj        ← provided by subject (bonus 1 reference)
    │   └── cube.obj                  ← simple test object (hand-written)
    │
    └── textures/
        ├── ponies.bmp                ← cheerful texture (subject requirement)
        └── test.ppm                  ← generated test texture (P6 binary)
```

---

## File Responsibilities

### Root

| File | Purpose |
|---|---|
| `Makefile` | Wraps `dotnet publish`. Exposes `all`, `clean`, `fclean`, `re` |
| `scop.csproj` | .NET 8 project file. Only external dependency: OpenTK 4 NuGet |
| `README.md` | How to build, how to run, controls reference |

---

### src/Math/

All hand-rolled. Zero dependencies on OpenTK math. Required by subject.

| File | Key members |
|---|---|
| `Vec2.cs` | `X`, `Y` — operators `+`, `-`, `*`, `/` |
| `Vec3.cs` | `X`, `Y`, `Z` — `Dot()`, `Cross()`, `Normalized()`, `Length()` |
| `Vec4.cs` | `X`, `Y`, `Z`, `W` — `Xyz` property |
| `Mat4.cs` | `Identity()`, `Translate()`, `Scale()`, `RotateX/Y/Z()`, `Perspective()`, `LookAt()`, `operator*`, `ToArray()` |

---

### src/Parsing/

| File | Purpose |
|---|---|
| `Vertex.cs` | Struct with `Position:Vec3`, `Color:Vec3`, `TexCoord:Vec2`. `Stride = 32` |
| `ObjParser.cs` | Reads `v`, `vt`, `vn`, `f`. Centers on centroid. Delegates triangulation to `ITriangulator`, UV to `IUvMapper` |
| `Interfaces/ITriangulator.cs` | Strategy interface — `Triangulate(polygon) : List<Triangle>` |
| `Interfaces/IUvMapper.cs` | Strategy interface — `Map(pos, normal, min, max) : Vec2` |
| `Triangulation/FanTriangulator.cs` | Fan triangulation — mandatory, convex faces only |
| `Triangulation/EarClipper.cs` | Ear-clipping — bonus 1, handles concave + non-coplanar |
| `UvMapping/BoxUvMapper.cs` | Global XY projection — mandatory fallback |
| `UvMapping/FaceNormalUvMapper.cs` | Per-face normal projection — bonus 2, no stretching |

---

### src/Utils/

Shared utilities — used by both `Parsing` and `Rendering` modules. No dependencies on OpenGL or OpenTK.

| File | Key members | Used by |
|---|---|---|
| `FileValidator.cs` | `Validate(path, caller)` — exists + readable check, specific error per case | `ObjParser`, `Texture`, `Shader` |
| `FileParser.cs` | `ParseFloat(s)` — locale-safe. `TokenizeLine(line)` — strips `#` comments, splits. `ReadPpmToken(reader)` — binary stream token reader | `ObjParser`, `Texture` |

---

### src/Rendering/

| File | Purpose |
|---|---|
| `Shader.cs` | `Load(vert, frag)`, `Use()`, `SetInt/Float/Vec3/Mat4()` |
| `Mesh.cs` | `Upload(ObjMesh)`, `Draw()` — manages VAO, VBO, EBO lifecycle |
| `Interfaces/ITexture.cs` | Null Object interface — `Bind(unit)`, `Unbind()`, `IsLoaded` |
| `Texture.cs` | Real texture — `FromFile(path)` factory, `LoadBMP()`, `LoadPPM()`, GPU upload |
| `NullTexture.cs` | Null Object — silent no-op Bind/Unbind, `IsLoaded = false` |

---

### src/

| File | Purpose |
|---|---|
| `App.cs` | Thin orchestrator — owns `AppState`, extends `GameWindow`, delegates to `InputHandler` and `Renderer` |
| `AppState.cs` | Plain data container — all runtime state in one place: assets, transform, rotation, blend, drag |
| `InputHandler.cs` | Reads `KeyboardState` + mouse events. Mutates `AppState`. No OpenGL calls |
| `Renderer.cs` | Builds `model`, `view`, `projection` matrices. Sets uniforms. Calls `Mesh.Draw()`. No input logic |
| `Program.cs` | `Main(args)` — validates args, prints usage, creates and runs `App` |

---

### shaders/

| File | Purpose |
|---|---|
| `vertex.glsl` | Inputs: `a_position`, `a_color`, `a_texCoord`. Uniforms: `u_model`, `u_view`, `u_projection`. Outputs `v_color`, `v_texCoord` |
| `fragment.glsl` | `mix(vec4(v_color,1.0), texture(u_texture, v_texCoord), u_blendFactor)` |

---

### assets/models/

| File | Used for |
|---|---|
| `42logo.obj` | Mandatory defense — must spin around centroid |
| `teapot_original.obj` | Bonus 1 — concave faces, must render without artifacts with ear-clipping |
| `teapot_blender.obj` | Bonus 1 — clean reference version to compare against |
| `cube.obj` | Development testing — simple known geometry |

---

### assets/textures/

| File | Used for |
|---|---|
| `ponies.bmp` | Mandatory defense — cheerful texture toggled with T |
| `test.ppm` | Development testing — generated programmatically |

---

## Vertex Buffer Layout

Every vertex is 32 bytes, interleaved in a single VBO:

```
Bytes  0–11   →  location 0  →  Position  (Vec3 = 3 × float)
Bytes 12–23   →  location 1  →  Color     (Vec3 = 3 × float)
Bytes 24–31   →  location 2  →  TexCoord  (Vec2 = 2 × float)
```

---

## App State Fields

All runtime state lives in `AppState.cs`. `InputHandler` and `Renderer` receive it as a parameter — no global state, no circular dependencies.

```
Rendering
  Shader    _shader
  Mesh      _mesh
  ITexture  _texture    ← holds Texture or NullTexture

Transform
  Vec3      _position       ← keyboard translation
  float     _rotAngle       ← auto Y-rotation accumulator
  float     _cameraZ        ← zoom distance (bonus 3)

Manual rotation (bonus 4)
  float     _manualRotX     ← accumulated from mouse drag
  float     _manualRotY     ← accumulated from mouse drag
  bool      _isDragging
  Vec2      _mouseLastPos

Texture blend
  float     _blendFactor    ← current animated value [0..1]
  float     _blendTarget    ← 0 = color, 1 = texture
  bool      _textureOn
```

---

## Build Flow

```
make
  └─ dotnet publish -c Release -o ./build
       └─ restores OpenTK from NuGet (first run only)
       └─ compiles all src/**/*.cs
       └─ outputs binary to ./build/scop
  └─ ln -sf build/scop scop        ← symlink at root for easy run
```

---

## Run

```bash
./scop assets/models/42logo.obj assets/textures/ponies.bmp
```

---

## Linux Dependencies

```bash
# .NET SDK
sudo apt-get install dotnet-sdk-8.0

# OpenGL system libraries (usually pre-installed)
sudo apt-get install libgl1 libglu1-mesa

# WSL-specific: if you see "Failed to create OpenGL context"
# make sure WSLg is enabled or use an X server (VcXsrv / Xming)
export DISPLAY=:0   # only needed on older WSL2 without WSLg
```

---

## WSL Notes

| Issue | Fix |
|---|---|
| No display on WSL2 | Enable WSLg (Windows 11) or install VcXsrv on Windows |
| OpenGL version too low | WSL2 + WSLg supports OpenGL 4.1 via Mesa — should work out of the box |
| `libGL.so` not found | `sudo apt-get install libgl1-mesa-glx` |
| Slow rendering on WSL | Normal — WSL GPU passthrough adds overhead. Test final performance on native Ubuntu |