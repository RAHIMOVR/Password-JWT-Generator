# Password-JWT-Generator

A cryptographically secure password and token generator for the terminal, written in C#.

PassGen lets you control the length, character sets and target entropy (up to 256 bits) of every password it creates. It can also generate random tokens in hex, Base64, Base64Url or Base32 for API keys, session IDs and JWT signing secrets (a 256-bit token is a suitable key for HS256).

## Features

- Secure randomness from `System.Security.Cryptography.RandomNumberGenerator` (never `System.Random`)
- Adjustable length (1-1024 characters)
- Target entropy mode: ask for 128 or 256 bits and the length is calculated for you
- Choice of character sets: lowercase, uppercase, digits, symbols
- Guarantees at least one character from each selected set, without biasing the result
- Option to exclude look-alike characters (`0 O 1 l I |`)
- Custom character sets
- Token mode with hex, Base64, Base64Url and Base32 output
- Entropy and strength rating shown for every password
- Interactive mode for step-by-step generation
- Pipe-friendly: passwords go to stdout, info goes to stderr
- Clears generated secrets from memory after use
- Single file, no external dependencies

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download) or newer

## Getting started

```bash
git clone https://github.com/RAHIMOVR/Password-JWT-Generator.git
cd Password-JWT-Generator
dotnet run -- --help
```

To build a standalone executable:

```bash
dotnet publish -c Release -o publish
./publish/PassGen --help
```

## Usage

```
passgen [options]          generate password(s)
passgen token [options]    generate random token(s)
passgen -i                 interactive mode
```

When running through `dotnet run`, put your options after `--`, for example `dotnet run -- -l 24`.

### Password options

| Option | Description | Default |
| --- | --- | --- |
| `-l`, `--length <n>` | Password length, 1-1024 | 16 |
| `-b`, `--bits <n>` | Target entropy in bits, 8-256 (sets the length automatically) | |
| `-c`, `--charset <sets>` | Any of `l` (lower), `u` (upper), `d` (digits), `s` (symbols), or `all` | `luds` |
| `--custom <chars>` | Use exactly these characters instead of `--charset` | |
| `-x`, `--no-ambiguous` | Exclude look-alike characters: `0 O 1 l I \|` | off |
| `--no-require` | Do not force one character from each selected set | off |

### Token options

| Option | Description | Default |
| --- | --- | --- |
| `-b`, `--bits <n>` | Random bits, 8-256 | 256 |
| `-f`, `--format <fmt>` | `hex`, `base64`, `base64url` or `base32` | `hex` |

### Common options

| Option | Description | Default |
| --- | --- | --- |
| `-n`, `--count <n>` | How many values to generate, 1-100 | 1 |
| `-q`, `--quiet` | Print only the values, without entropy info | off |
| `-i`, `--interactive` | Answer prompts instead of passing options | |
| `-h`, `--help` | Show help | |
| `-v`, `--version` | Show version | |

## Examples

Generate a 24-character password:

```bash
passgen -l 24
```

Generate a password with 128 bits of entropy using letters and digits only, without look-alike characters:

```bash
passgen -b 128 -c lud -x
```

Generate a 256-bit URL-safe token:

```bash
passgen token -b 256 -f base64url
```

Generate a secret for signing JWTs with HS256:

```bash
passgen token -b 256 -f base64
```

Generate five passwords and save them to a file:

```bash
passgen -n 5 -q > passwords.txt
```

Generate a numeric PIN:

```bash
passgen -l 6 -c d
```

Sample output:

```
$ passgen -l 20
k#8Vq]tR2m;Xe7_pLz!w
length 20 | pool 89 chars | ~129.5 bits of entropy (excellent)
```

## How it works

### Entropy

The entropy of a random password is:

```
entropy = length x log2(pool size)
```

When you use `--bits`, PassGen solves this for the length and rounds up, so the result always meets or exceeds your target.

Lengths needed for common targets:

| Character sets | Pool size | 128 bits | 256 bits |
| --- | --- | --- | --- |
| Digits (`d`) | 10 | 39 | 78 |
| Lowercase (`l`) | 26 | 28 | 55 |
| Letters and digits (`lud`) | 62 | 22 | 43 |
| All (`luds`) | 89 | 20 | 40 |

Strength ratings:

| Entropy | Rating |
| --- | --- |
| under 40 bits | weak |
| 40-59 bits | fair |
| 60-79 bits | strong |
| 80-127 bits | very strong |
| 128 bits and above | excellent |

### Requiring every character set

To guarantee at least one character from each selected set, PassGen uses rejection sampling: it generates a full password and starts over if a set is missing. Unlike inserting a required character at a fixed or random position, this keeps every valid password equally likely.

### Tokens

Tokens are raw random bytes encoded in the chosen format. Bit counts that are not a multiple of 8 are rounded up to whole bytes. Base64Url and Base32 output is unpadded.

| Format | Length of a 256-bit token |
| --- | --- |
| hex | 64 characters |
| base64 | 44 characters |
| base64url | 43 characters |
| base32 | 52 characters |

### Symbols

The symbol set is `!@#$%^&*()-_=+[]{};:,.<>?/~`. Quotes, backticks and backslashes are left out so passwords can be pasted into shells and config files without escaping.

## License

This project is licensed under the MIT License.
