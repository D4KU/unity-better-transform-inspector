This is a Unity add-on to improve the Inspector of Transform components. It
integrates a powerful expression engine that gives you more control over
every selected object. For example, now you can:

- Add to each object's current x position
- Separate objects by a uniform distance
- Swap x and y rotations
- Place objects in a circle
- Scale objects randomly
- And much more...


# Installation

In your project folder, simply add this to the dependencies inside `Packages/manifest.json`:

`"com.d4ku.better-transform-inspector": "https://github.com/D4KU/unity-better-transform-inspector.git"`

Alternatively, you can:
* Clone this repository
* In Unity, go to `Window` > `Package Manager` > `+` > `Add Package from disk`
* Select `package.json` at the root of the package folder

Unity should automatically load the add-on. After the successful installation
you should already see additional input fields for world space coordinates in
the inspector of a Transform component. The expression engine needs to be
manually activated, though. To do so, open the context menu of a Transform
component and click `Enable Expressions`.


# Usage

The add-on defines various variables and functions to be used in the
inspector's input fields. What follows is a comprehensive list of these
variables and functions. Of course the infix operators `+-*/` for addition,
subtraction, multiplication, and division are also supported.


### Variables

| Name | Description |
| ---- | ----------- |
| x/y/z | x/y/z-coordinate of the edited vector (position/rotation/scale) * |
| px/py/pz | x/y/z-coordinate of the object's position * |
| rx/ry/rz | x/y/z-coordinate of the object's rotation * |
| sx/sy/sz | x/y/z-coordinate of the object's scale * |
| i | The object's index in the list of selected objects |
| j | The object's [sibling index](https://docs.unity3d.com/ScriptReference/Transform.GetSiblingIndex.html) |
| l | The number of selected objects |
| c | The object's [child count](https://docs.unity3d.com/ScriptReference/Transform-childCount.html) |
| r | [Random value](https://docs.unity3d.com/ScriptReference/Random-value.html) in the range [0, 1] |
| pi | [pi](https://learn.microsoft.com/en-us/dotnet/api/system.math.pi) constant |
| e | [e](https://learn.microsoft.com/en-us/dotnet/api/system.math.e) constant |

\* Lowercase x, y, z returns the local coordinate, whereas the uppercase character returns the global coordinate.


### Functions

| Signature | Return value |
| --------- | ------------ |
| x(n) | n'th selected object's x-coordinate of the edited vector. Likewise for y and z. * |
| px(n) | n'th selected object's x-position. Likewise for y and z. * |
| rx(n) | n'th selected object's x-rotation. Likewise for y and z. * |
| sx(n) | n'th selected object's x-scale. Likewise for y and z. * |
| abs(x) | [Absolute value](https://learn.microsoft.com/en-us/dotnet/api/system.math.abs) of x |
| sqrt(x) | [Square root](https://learn.microsoft.com/en-us/dotnet/api/system.math.sqrt) of x |
| pow(x, y) | [x to the power of y](https://learn.microsoft.com/en-us/dotnet/api/system.math.pow) |
| mod(x, y) | [Remainder](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/expressions#12104-remainder-operator) of x / y |
| log(x) | [Natural logarithm](https://learn.microsoft.com/en-us/dotnet/api/system.math.log) of x |
| log(x, y) | [Logarithm](https://learn.microsoft.com/en-us/dotnet/api/system.math.log) of x to the base of y |
| trunc(x) | [Integral part](https://learn.microsoft.com/en-us/dotnet/api/system.math.truncate) of x |
| frac(x) | Fractional part of x |
| sign(x) | [-1 if x is negative, 1 otherwise](https://learn.microsoft.com/en-us/dotnet/api/system.math.sign) |
| step(x, y) | 1 if x >= y, 0 otherwise |
| round(x) | [Rounded value](https://learn.microsoft.com/en-us/dotnet/api/system.math.round) of x |
| floor(x) | [Rounded down value](https://learn.microsoft.com/en-us/dotnet/api/system.math.floor) of x |
| ceil(x) | [Rounded up value](https://learn.microsoft.com/en-us/dotnet/api/system.math.ceiling) of x |
| clamp(x, a, b) | x clamped to range [a, b] |
| quant(x, y) | Quantize: largest multiple of y <= x |
| min(...) | [Minimum](https://learn.microsoft.com/en-us/dotnet/api/system.linq.enumerable.min) of every passed argument |
| max(...) | [Maximum](https://learn.microsoft.com/en-us/dotnet/api/system.linq.enumerable.max) of every passed argument |
| avg(...) | [Average](https://learn.microsoft.com/en-us/dotnet/api/system.linq.enumerable.average) of every passed argument |
| sin(x) | [Sine](https://learn.microsoft.com/en-us/dotnet/api/system.math.sin) of x |
| cos(x) | [Cosine](https://learn.microsoft.com/en-us/dotnet/api/system.math.cos) of x |
| tan(x) | [Tangent](https://learn.microsoft.com/en-us/dotnet/api/system.math.tan) of x |
| asin(x) | [Arcsine](https://learn.microsoft.com/en-us/dotnet/api/system.math.asin) of x |
| acos(x) | [Arccosine](https://learn.microsoft.com/en-us/dotnet/api/system.math.acos) of x |
| atan2(x, y) | [Arctangent](https://learn.microsoft.com/en-us/dotnet/api/system.math.atan2) of x |
| pyth(a, b) | [Pythagorean equation](https://en.wikipedia.org/wiki/Pythagorean_theorem): sqrt(a * a + b * b) |
| rand(a, b) | [Random value](https://docs.unity3d.com/ScriptReference/Random.Range.html) in the range [a, b] |

\* Lowercase x, y, z returns the local coordinate, whereas the uppercase character returns the global coordinate.
