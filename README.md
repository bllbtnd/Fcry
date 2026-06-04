# Fcry

A cross-platform file encryption desktop app built with .NET 10 and Avalonia UI 11. Encrypt and decrypt files and folders using AES-256-GCM with a passphrase-derived master key that lives only in memory.

![Platform](https://img.shields.io/badge/platform-macOS%20%7C%20Windows%20%7C%20Linux-lightgrey)
![.NET](https://img.shields.io/badge/.NET-10.0-purple)
![Avalonia](https://img.shields.io/badge/Avalonia-11-blue)

---

## Features

- **Passphrase-based authentication** — master key derived with Argon2id, never written to disk
- **AES-256-GCM encryption** — authenticated encryption with a unique key and IV per file or folder
- **Folder encryption** — drop a folder to pack and encrypt it as a single `.fcry` file; decrypt to restore the original folder
- **Drag and drop** — drop any file or folder to encrypt; drop a `.fcry` file to decrypt
- **Multi-passphrase friendly** — unlock with any passphrase; files encrypted with a different passphrase will fail decryption gracefully with a clear error
- **Bulk operations** — process multiple files and folders at once with per-item progress
- **Optional key file** — XOR the master key with SHA-256 of a key file for two-factor security
- **Auto-lock** — session locks after 5 minutes of inactivity, wiping the key from memory
- **Inactivity countdown** — visible 60-second warning before auto-lock triggers
- **Manual lock** — lock button always visible in the top bar

---

## How it works

### Key derivation

On unlock, Argon2id derives a 32-byte master key from the passphrase and a random salt stored in the app config. The salt is the only thing persisted — the key exists only in memory for the duration of the session.

```
passphrase + stored_salt → Argon2id → master_key (memory only)
```

Because there is no passphrase verification stored on disk, you can unlock with any passphrase. If you use a different passphrase than the one used to encrypt a file, decryption fails with an authentication error — nothing is corrupted, nothing is exposed.

### Per-file encryption

Each file (or folder archive) gets a fresh random salt and IV. HKDF-SHA256 derives a unique encryption key from the master key and that salt — the master key is never used directly for encryption.

```
master_key + random_salt → HKDF-SHA256 → file_key → AES-256-GCM(file_key, iv, plaintext)
```

### Folder encryption

When a folder is dropped, Fcry zips it with `System.IO.Compression`, encrypts the ZIP blob with the same AES-GCM scheme, and stores the folder name (with a trailing `/` marker) in the header. Decryption detects the marker, extracts the ZIP, and recreates the original folder structure.

### File format

```
[4 bytes]  Magic: 0x46 0x43 0x52 0x59  ("FCRY")
[1 byte]   Version: 0x01
[32 bytes] Per-file salt (used in HKDF)
[12 bytes] IV (nonce)
[8 bytes]  Original name length (big-endian)
[N bytes]  Original name — plain filename, or "foldername/" for folder archives
[rest]     Ciphertext + 16-byte GCM authentication tag
```

The magic bytes allow Fcry to auto-detect whether a dropped `.fcry` file should be decrypted — no toggle needed.

### Key material handling

All sensitive byte buffers are zeroed with `CryptographicOperations.ZeroMemory` immediately after use:

| Material | Zeroed when |
|---|---|
| Passphrase bytes | Immediately after Argon2id call |
| Master key | On lock, on dispose |
| Per-file HKDF key | In `finally` after every operation |
| Decrypted plaintext | In `finally` after writing to disk/folder |

---

## Getting started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10)

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

---

## Project structure

```
Fcry/
├── Core/                        # Class library — zero Avalonia dependency
│   ├── Crypto/
│   │   ├── ArgonKeyDerivation   # Argon2id (4 iter / 64 MB / 2 threads)
│   │   ├── HkdfDerivation       # HKDF-SHA256 per-file key derivation
│   │   ├── AesGcmCipher         # AES-256-GCM encrypt / decrypt
│   │   └── MasterKeyManager     # In-memory key holder, ZeroMemory on lock
│   ├── IO/
│   │   ├── FileEncryptor        # File + folder → .fcry (shared encrypt-bytes core)
│   │   ├── FileDecryptor        # .fcry → file or folder (ZIP extraction)
│   │   └── ConfigManager        # Persists Argon2 salt
│   └── Models/
│       ├── FileHeader           # Magic bytes, version, header size constants
│       ├── AppConfig            # Argon2 salt
│       └── CryptoResult         # Ok() / Fail(error)
└── App/                         # Avalonia UI — MVVM, no business logic in code-behind
    ├── ViewModels/
    │   ├── MainWindowViewModel  # Switches between lock and main screens
    │   ├── LockScreenViewModel  # Argon2 derivation, optional key file XOR
    │   ├── MainScreenViewModel  # Queue drain, folder detection, inactivity timer
    │   └── FileQueueItem        # Per-item state, progress, folder flag
    ├── Views/
    │   ├── MainWindow           # Pointer/key events → inactivity reset
    │   ├── LockScreenView       # Passphrase input, key file picker, Enter-to-unlock
    │   └── MainScreenView       # Drag-drop zone (files + folders), queue list
    └── Converters/
        └── AppConverters        # FuncValueConverters for status colors and visibility
```

---

## Security notes

- The master key is **never written to disk** in any form
- There is **no passphrase verification token** stored on disk — any passphrase unlocks the session; wrong-passphrase detection happens at decryption via GCM authentication tag failure
- Key file mode XORs the master key with `SHA-256(key_file_bytes)` — losing the key file means losing access to files encrypted with it
- The GCM tag detects both wrong passphrase and file corruption — decryption fails explicitly, never producing garbage output

---

## Argon2id parameters

| Parameter | Value |
|---|---|
| Iterations | 4 |
| Memory | 64 MB |
| Parallelism | 2 |
| Output length | 32 bytes |

---

## Dependencies

| Package | Purpose |
|---|---|
| [Avalonia 11](https://avaloniaui.net) | Cross-platform UI framework |
| [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) | MVVM source generators |
| [Konscious.Security.Cryptography.Argon2](https://github.com/kmaragon/Konscious.Security.Cryptography) | Argon2id implementation |

All other cryptographic primitives (AES-GCM, HKDF, SHA-256) use .NET 10's built-in `System.Security.Cryptography`. Folder archiving uses the built-in `System.IO.Compression`.

---

## License

MIT
