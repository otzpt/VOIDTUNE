# Contributing

1. Fork the repository
2. Branch: `git checkout -b feature/your-feature`
3. Commit and push
4. Open a pull request

CI must pass: build, full test suite, release-packaging validation.

## Adding a tweak

Add it to `VOIDTUNE.WinUI/Services/TweakCatalog.cs`, following the existing entries. Include a revert command wherever the underlying change supports one. New tweaks should have a clear, verifiable reason to exist — see [tweaks.md](tweaks.md) for the bar tweaks are held to. Several have already been removed after turning out to be placebo or harmful; see [../CHANGELOG.md](../CHANGELOG.md).

## Bug reports

Open an issue with your Windows build, hardware, and the relevant lines from `%LocalAppData%\VOIDTUNE\logs\`. When a tweak fails inside the app, use the failure popup's "Report on GitHub" button — it pre-fills the issue.
