# ⚠️ Project Status

**This project is superseded by another implementation.**

I no longer use it myself and have developed a Toggl client from scratch in Rust
as a replacement. If you feel interested, please give it a try! It's free and available at
https://github.com/sterliakov/toggl
(and also supports easier installation methods - binary download, `npm` and `cargo`!)

This is a fork of a discontinued Toggl app. The sole purpose of this repository 
is to keep it working under linux - apparently, upstream maintainers 
no longer care about us.

It may build or not build on other platforms. I'm using this on Ubuntu only, 
but any other distros are more than welcome - please open pull requests 
explaining build details on other distros. I'll try to review any test your 
suggestions promptly, and your recipes will be added to this README.

If you feel interested in testing or fixing builds on platforms other than
linux - contributions are welcome. Please be aware that I won't be able to test
Windows or MacOS builds, so the recipes will likely either stay in issues/PRs
or be incorporated with "not tested, not my responsibility" disclaimer and 
showing your attribution.

# Recipes

## Ubuntu installation

I won't claim that this is the only/the best way to build. This is what works for me. 

System libraries (this list may be non-exhaustive, but is enough to build in ubuntu:22.04 image):

```sh
apt-get update
apt-get install -y --no-install-recommends \
    git build-essential cmake libpoco-dev \
    libjsoncpp-dev libxmu-headers libssl-dev libxss-dev \
    qtbase5-private-dev libqt5x11extras5-dev libqt5networkauth5-dev qtbase5-dev
```

Build:

```sh
set -euo pipefail
git clone https://github.com/sterliakov/toggldesktop --depth 1
cd toggldesktop
mkdir -p build && cd build
cmake -DTOGGL_PRODUCTION_BUILD=ON -DUSE_BUNDLED_LIBRARIES=OFF ..
make -j8  # Adjust process limit to your system
# Run the executable
./src/ui/linux/TogglDesktop/TogglDesktop
```

Now copy the binary to whatever directory you like on PATH, and you're good to go! E.g.

```sh
cp ./src/ui/linux/TogglDesktop/TogglDesktop ~/.local/bin/toggl
```

# MacOS and Windows

* [ ] 🍏 [MacOS Toggl Track](https://toggl.com/track/time-tracking-mac)
* [ ] 🖥 [Windows Toggl Track](https://toggl.com/track/time-tracking-windows/)

# Original README

<h1></h1>

<h1 align="center">
  <a href="https://toggl.com"><img src="https://raw.githubusercontent.com/toggl-open-source/toggldesktop/gh-pages/assets/toggl-track-wide.png" alt="Toggl Track"></a>
</h1>

<h4 align="center">Native desktop applications for the leading time tracking tool <a href="https://toggl.com" target="_blank">Toggl</a>.</h4>

<p align="center">
    <a href="https://github.com/toggl-open-source/toggldesktop/commits/master">
    <img src="https://img.shields.io/github/last-commit/toggl-open-source/toggldesktop.svg?style=flat&logo=github&logoColor=white"
         alt="GitHub last commit">
    <a href="https://github.com/toggl/toggldesktop/issues">
    <img src="https://img.shields.io/github/issues-raw/toggl-open-source/toggldesktop.svg?style=flat&logo=github&logoColor=white"
         alt="GitHub issues">
    <a href="https://github.com/toggl/toggldesktop/pulls">
    <img src="https://img.shields.io/github/issues-pr-raw/toggl-open-source/toggldesktop.svg?style=flat&logo=github&logoColor=white"
         alt="GitHub pull requests">
    <img src="https://img.shields.io/badge/licence-BSD--3-green"
         alt="Licence BSD-3">
</p>

<p align="center">
  <a href="#about">About</a> •
  <a href="#download">Download</a> •
  <a href="#build">Build</a> •
  <a href="#change-log">Change log</a> •
  <a href="#contribute">Contribute</a>
</p>

# About

  **Toggl Desktop** is a Toggl time tracking client with many helper functions that make tracking time more effortless and smooth. Features such as Idle detection, reminders to track and Pomodoro Timer make this app a great companion when productivity and efficiency is the goal.

<img src="https://user-images.githubusercontent.com/842229/63856838-3a869580-c9ab-11e9-9e36-7db23059ce29.png"
         alt="Toggl Desktop apps">

# Download

Toggl built and signed apps for all platforms

## Mac

<br>
<a href="https://toggl.github.io/toggldesktop/download/macos-stable/">64bit dmg</a>&nbsp;&nbsp;&nbsp;&nbsp;|&nbsp;&nbsp;&nbsp;&nbsp;<a href='https://itunes.apple.com/ee/app/toggl-desktop/id957734279?mt=12'>
  Mac App Store</a>
<br/>
<br/>
<i>Officially macOS 10.11 and newer stable macOS versions are supported.</i>

## Windows

<br/>
<a href="https://toggl.github.io/toggldesktop/download/windows64-stable/">64bit installer</a>&nbsp;&nbsp;&nbsp;&nbsp;|&nbsp;&nbsp;&nbsp;&nbsp;<a href="https://toggl.github.io/toggldesktop/download/windows-stable/">32bit installer</a>&nbsp;&nbsp;&nbsp;|&nbsp;&nbsp;&nbsp;&nbsp;<a href="https://chocolatey.org/packages/toggl">Chocolatey</a>&nbsp;&nbsp;&nbsp;&nbsp;|&nbsp;&nbsp;&nbsp;&nbsp;<a href='//www.microsoft.com/store/apps/9nk3rf9nbjnp?cid=storebadge&ocid=badge'>Microsoft Store</a>
<br/>
<br/>
<i>App has been tested on Windows 7, 8, 8.1 and 10. Toggl Desktop Windows app has not been tested on Surface type touchscreen environments.</i>

## Linux

<br>
<a href="https://toggl.github.io/toggldesktop/download/linux_tar.gz-stable/">Tarball</a>&nbsp;&nbsp;&nbsp;&nbsp;|&nbsp;&nbsp;&nbsp;&nbsp;<a href='https://flathub.org/apps/details/com.toggl.TogglDesktop'>Flathub</a>&nbsp;&nbsp;&nbsp;&nbsp;
<br/>
<br/>
<i>Only 64bit is supported</i>

# Build

Please check OS specific requirements below.

_By default the app builds for testing server. To use the compiled app with live server see this guide [https://github.com/toggl-open-source/toggldesktop/wiki/Building-Toggl-Desktop-from-source-for-usage-with-live-servers](https://github.com/toggl-open-source/toggldesktop/wiki/Building-Toggl-Desktop-from-source-for-usage-with-live-servers)_

## macOS
### Requirements
- macOS 11+, Xcode 12.2+ and Swift 5+
- Install Bundler
```bash
$ sudo gem install bundler
```

### Build
```bash
# Prepare cocoapod
$ make init_cocoapods
```
Run `bundle exec pod repo update` in case there is an error about out-of-date source repos (some pod version is missing).

- Open workspace at `src/ui/osx/TogglDesktop.xcworkspace`
- Select TogglDesktop scheme and build.

## Linux

### Dependencies

You'll need these Qt (at version 5.12 or higher) modules: QtWidgets (with private headers), QtNetwork, QtNetworkAuth, QtDBus, QtX11Extras

If Qt is not installed from your distribution's package manager, you will need to set the `CMAKE_PREFIX_PATH` environment variable to point to the `lib/cmake` folder in the Qt version you wish to use.

These dependencies are mandatory:
 * libXScrnSaver (`libxss-dev` in deb-based distros and `libXScrnSaver-devel` in rpm-based)

 You can install them all in debian with a command:
```bash
 $ sudo apt install libxss-dev build-essential libgl-dev libreadline-dev

 ```
 
These dependencies are optional and will be bundled if the `USE_BUNDLED_LIBRARIES` CMake argument is set or your system does NOT have their development packages installed:
 * POCO
 * Lua
 * jsoncpp
 * Qxt

These libraries will be bundled regardless of your system:
 * bugsnag-qt
 * qt-oauth-lib

### Build the app

*in the toggldesktop source tree root*
```bash
mkdir -p build && pushd build             # Create build directory
cmake ..                                  # Setup cmake configs
make -j8                                  # Build the app. The number defines the count of parallel jobs (number of your CPU cores is a good value for that)
./src/ui/linux/TogglDesktop/TogglDesktop  # Run the built app
```

## Windows

Install Visual Studio 2019 with `.NET desktop development`, `Desktop development with C++` and `Universal Windows Platform development` components checked during installation. You can download free Visual Studio Community [here](https://visualstudio.microsoft.com/vs/community/).

Then open the solution file `src\ui\windows\TogglDesktop\TogglDesktop.sln` and run it in `Debug` mode.

The solution is using OpenSSL binaries. To rebuild OpenSSL from sources refer to [this page](docs/win/build-openSSL.md).


# Change log

Change log can be viewed at [http://toggl.github.io/toggldesktop/](http://toggl.github.io/toggldesktop/)

# Contribute

Before sending us a pull request, please format the source code:

```bash
$ make fmt
```

Also, please check for any cpplint issues:

```bash
$ make lint
```

Check if unit tests continue to pass:

```bash
$ make test
```

