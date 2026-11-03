# Changelog

All notable changes to this package are documented in this file.

## [0.3.0] - 2026-11-03

### Fixed

- `PaginatedDataSource.Reset()` no longer skips page 0 on the next load.

### Changed

- Updated to Tools Core `0.3.0`, which removes `IMaskSource`.

### Documentation

- Added recipes for expandable cells, pagination, grids, headers, reversed lists, filtered data and pull to refresh.
- Added a `ListView` comparison and a description of the code layout.
- Added contributing, security and issue template files, and a Quick Start sample.
- Removed internal development notes from code comments, and added XML documentation to the main scroller APIs.

## [0.2.0] - Unreleased

### Changed

- **Breaking:** Renamed the namespaces from `Tools.RecyclableScroller.*` to `RecyclableScroller.*`. Main types are now in `RecyclableScroller.Runtime`.
- **Breaking:** Renamed the assemblies to `RecyclableScroller`, `RecyclableScroller.Core`, `RecyclableScroller.Abstractions`, `RecyclableScroller.UGUI`, `RecyclableScroller.UIToolkit` and `RecyclableScroller.Editor`.
- **Breaking:** Renamed the `GUI` module to `UGUI`.

## [0.1.1] - Unreleased

- Updated to Tools Core `0.2.0`.

## [0.1.0] - Unreleased

First pre-release for private testing.
