---
title: Getting Started
nav_order: 1
---

# Getting Started


<a id="download-zip" class="btn btn-primary" href="https://github.com/miskibin/quickLink/releases/latest">Download Win x64 (.zip)</a>

<script>
(function () {
	var owner = 'miskibin';
	var repo = 'quickLink';
	var link = document.getElementById('download-zip');
	if (!link) return;

	var fallback = 'https://github.com/' + owner + '/' + repo + '/releases/latest';
	link.href = fallback;

	fetch('https://api.github.com/repos/' + owner + '/' + repo + '/releases/latest')
		.then(function (r) { return r.ok ? r.json() : null; })
		.then(function (release) {
			if (!release || !release.tag_name) return;
			var tag = String(release.tag_name);
			var version = tag.replace(/^v/i, '');
			var assetName = 'quickLink-' + version + '-win-x64.zip';

			if (Array.isArray(release.assets)) {
				var match = release.assets.find(function (a) { return a && a.name === assetName && a.browser_download_url; });
				if (match) {
					link.href = match.browser_download_url;
					return;
				}
			}

			link.href = 'https://github.com/' + owner + '/' + repo + '/releases/download/' + tag + '/' + assetName;
		})
		.catch(function () {
			link.href = fallback;
		});
})();
</script>

## Installation

1. Download the latest release
2. Extract and run `QuickLink.exe`

> QuickLink starts minimized to the system tray by default. If it doesn’t appear immediately, wait a couple of seconds for startup.
{: .note }

## Basic Workflow

Press <kbd>Ctrl+Shift+A</kbd> → Type to search → Press <kbd>Enter</kbd> to execute

## Keyboard Shortcuts

| Action | Shortcut |
|--------|----------|
| Open/Close | <kbd>Ctrl+Shift+A</kbd> |
| Navigate | <kbd>↑</kbd> / <kbd>↓</kbd> |
| Execute | <kbd>Enter</kbd> |
| Close | <kbd>Escape</kbd> |

---

Next: see [Features](features.md).
