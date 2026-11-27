# Privacy policy

Flux 2.1.0 is a local Windows application by **sukiqu**. It does not require an
account, collect usage data, show advertising or download updates automatically.

- Media titles, artists and album artwork are read from Windows media sessions for
  display in memory. Flux does not save this information to disk.
- Volume is read and controlled through Windows.
- The keyboard hook handles only media, volume, Caps Lock, Num Lock and Scroll Lock
  events. Normal typed text is not recorded or stored.
- Preferences are saved locally in `%LOCALAPPDATA%\AkisFlyout\settings.xml`.
  This legacy name prevents upgrades from losing preferences.
- Optional startup behavior is managed locally and does not require administrator
  access.
- Any diagnostic information created by the application remains on the device and
  is not uploaded by Flux.

Logs created by older versions are not removed automatically; review existing logs
before sharing them.

To remove local data, exit Flux and remove its settings folder yourself. Disable
Start with Windows in the app first if enabled. Release packages do not include a
user's existing settings or logs.
