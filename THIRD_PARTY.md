# Third-party components

AirLink launcher sources use the MIT license in LICENSE. This does not relicense bundled third-party software.

The Windows distribution contains uxplay-windows 2.0.0.1736 (GPLv3) and its runtime dependencies under their respective licenses. Original license notices are retained in the runtime folder.

- Windows receiver source and release: https://github.com/leapbtw/uxplay-windows/tree/2.0.0.1736
- Original binary asset: https://github.com/leapbtw/uxplay-windows/releases/download/2.0.0.1736/uxplay-windows.zip
- Original asset SHA256: 9d3a51c15fc9db857351195e7eb7bbb21700d9ae25d936a54bcf8536b62cca18
- UxPlay engine: https://github.com/FDH2/UxPlay

Packaging change: bundled D3Dcompiler_47.dll is omitted so Windows uses its system version. Receiver binaries are otherwise unchanged. AirLink launches the receiver as a separate process.

Source links do not replace any corresponding-source obligations when redistributing GPL components; retain upstream notices and provide the required corresponding source for public binary redistribution.
