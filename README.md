# Fcry

A cross-platform file encryption desktop app built with .NET 9 and Avalonia UI 11. Encrypt and decrypt files using AES-256-GCM with a passphrase-derived master key that lives only in memory.

![Platform](https://img.shields.io/badge/platform-macOS%20%7C%20Windows%20%7C%20Linux-lightgrey)
![.NET](https://img.shields.io/badge/.NET-9.0-purple)
![Avalonia](https://img.shields.io/badge/Avalonia-11-blue)

---

## Features

- **Passphrase-based authentication** — master key derived with Argon2id, never written to disk
- **AES-256-GCM encryption** — authenticated encryption with a unique key and IV per file
- **Drag and drop** — drop any file to encrypt; drop a `.fcry` file to decrypt
- **Bulk operations** — process multiple files at once with per-file progress
- **Optional key file** — XOR the master key with SHA-256 of a key file for two-factor security
- **Auto-lock** — session locks after 5 minutes of inactivity, wiping the key from memory
- **Inactivity countdown** — visible 60-second warning before auto-lock triggers
- **Manual lock** — lock button always visible in the top bar

## Screenshots

| Lock screen | Main screen |
|---|---|
| ![Lock screen](docs/lock.png) | ![Main screen](docs/main.png) |

## How it works

### Key derivation

On unlock, Argon2id derives a 32-byte master key from the passphrase and a random salt stored in the app config. The salt is the only thing persisted — the key exists only in memory for the duration of the session.

```
passphrase + stored_salt → Argon2id → master_key (memory only)
```

### Per-file encryption

Each file gets a fresh random salt and IV. HKDF-SHA256 derives a unique encryption key from the master key and that salt — the master key is never used directly for encryption.

```
master_key + random_salt → HKDF-SHA256 → file_key → AES-256-GCM(file_key, iv, plaintext)
```

### File format

Encrypted files use a binary format with a self-describing header:

```
[4 bytes]  Magic: 0x46 0x43 0x52 0x59  ("FCRY")
[1 byte]   Version: 0x01
[32 bytes] Per-file salt (used in HKDF)
[12 bytes] IV (nonce)
[8 bytes]  Original filename length (big-endian)
[N bytes]  Original filename (UTF-8)
[rest]     Ciphertext + 16-byte GCM authentication tag
```

The magic bytes allow Fcry to auto-detect whether a dropped file should be encrypted or decrypted — no toggle needed.

### Key material handling

All sensitive byte buffers are zeroed with `CryptographicOperations.ZeroMemory` immediately after use:

| Material | Zeroed when |
|---|---|
| Passphrase bytes | Immediately after Argon2id call |
| Derived master key | On lock, on dispose, on wrong passphrase |
| Per-file HKDF key | In `finally` after every file operation |
| Decrypted plaintext | In `finally` after writing to disk |

## Getting started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9)

### Run

```bash
git clone https://github.com/yourname/Fcry
cd Fcry/App
dotnet run
```

### Build

```bash
dotnet build Fcry.sln
```

### Publish (macOS)

```bash
dotnet publish App/App.csproj -c Release -r osx-arm64 --self-contained
```

## Project structure

```
Fcry/
├── Core/                        # Class library — zero Avalonia dependency
│   ├── Crypto/
│   │   ├── ArgonKeyDerivation   # Argon2id wrapper
│   │   ├── HkdfDerivation       # HKDF-SHA256 per-file key derivation
│   │   ├── AesGcmCipher         # AES-256-GCM encrypt / decrypt
│   │   └── MasterKeyManager     # In-memory key holder with ZeroMemory on lock
│   ├── IO/
│   │   ├── FileEncryptor        # Writes encrypted file with header
│   │   ├── FileDecryptor        # Reads header, derives key, decrypts
│   │   └── ConfigManager        # Persists Argon2 salt and passphrase verification token
│   └── Models/
│       ├── FileHeader           # Magic bytes, version, size constants
│       ├── AppConfig            # Salt + HMAC verification token
│       └── CryptoResult         # Success / failure result type
└── App/                         # Avalonia UI — MVVM, no logic in code-behind
    ├── ViewModels/
    │   ├── MainWindowViewModel  # Switches between lock and main screens
    │   ├── LockScreenViewModel  # Handles unlock flow and key file XOR
    │   ├── MainScreenViewModel  # File queue, inactivity timer, auto-lock
    │   └── FileQueueItem        # Per-file state and progress
    ├── Views/
    │   ├── MainWindow           # Forwards pointer/key events for inactivity reset
    │   ├── LockScreenView       # Passphrase input, key file picker
    │   └── MainScreenView       # Drag-drop zone, file queue list
    └── Converters/
        └── AppConverters        # FuncValueConverters for status colors and visibility
```

## Security notes

- The master key is **never written to disk** in any form
- Passphrase verification uses `HMAC-SHA256(master_key, "fcry-verify")` stored in the config — if you forget your passphrase there is no recovery path
- Key file mode XORs the master key with `SHA-256(key_file_bytes)` before any derivation — losing the key file means losing access to all files encrypted with it
- The GCM authentication tag detects both wrong passphrase and file corruption — decryption fails explicitly with an error rather than producing garbage output

## Argon2id parameters

| Parameter | Value |
|---|---|
| Iterations | 4 |
| Memory | 64 MB |
| Parallelism | 2 |
| Output length | 32 bytes |

## Dependencies

| Package | Purpose |
|---|---|
| [Avalonia 11](https://avaloniaui.net) | Cross-platform UI framework |
| [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) | MVVM source generators |
| [Konscious.Security.Cryptography.Argon2](https://github.com/kmaragon/Konscious.Security.Cryptography) | Argon2id implementation |

All cryptographic primitives (AES-GCM, HKDF, HMAC, SHA-256) use .NET 9's built-in `System.Security.Cryptography`.

## License

MIT
