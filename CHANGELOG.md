# Changelog

All notable changes to Lavistech File Toolkit are documented in this file.

The format is inspired by [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the project aims to use [Semantic Versioning](https://semver.org/).

## [Unreleased]

## [1.0.0] - 2026-11-24

### Added
- Initial release of the offline Windows file toolkit.
- Bulk rename with preview, templates (`{name}`, `{ext}`, `{n}`, `{date}`, `{parent}`), and undo of the last apply.
- Duplicate finder using size and SHA-256, with recycle of extra copies.
- File hashing for MD5, SHA-1, SHA-256, and SHA-512.
- Folder compare, and one-way folder sync with an optional recycle of files that exist only in the destination.
- File metadata, including image size and PDF page count.
- Image resize, format conversion, and JPEG or WebP compression.
- PDF merge, split, and content-stream recompress.
- ZIP and 7Z create, plus list and extract for ZIP, 7Z, RAR, TAR, and GZ.
- Drag-and-drop onto the open tool.
