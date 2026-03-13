Plan: TcNo-Acc-Switcher Security Fixes & Enhancements
TL;DR: The codebase is a well-structured multi-project .NET 10 solution, but has 5 critical security vulnerabilities, several resource management bugs, and numerous code quality improvements that are all actionable today.

Phase 1: Critical Security Fixes (Must-Do)
XXE Vulnerability in Steam XML Parsing

SteamSwitcherFuncs.cs ~L617, L706 — XmlDocument.Load() fetches remote XML without disabling DTD processing. Vulnerable to XXE injection and SSRF. Fix: set XmlResolver = null and DtdProcessing = Prohibit.
Path Traversal via JSInvokable

GeneralInvocableFuncs.cs L141-148 — [JSInvokable] GiFileReadAllText(string file) accepts any file path from JavaScript. An attacker could read arbitrary system files. Fix: whitelist allowed directories, validate with Path.GetFullPath() + prefix check.
Command Injection in TASKKILL

Processes.cs L167 — Arguments = $"/C TASKKILL /F /T /IM {procName}*" passes unsanitized input to cmd.exe. Fix: validate procName with a strict regex whitelist (^[a-zA-Z0-9._\-\s]{1,260}$) or avoid cmd.exe entirely by using Process.GetProcessesByName().
Production DetailedErrors Enabled

Startup.cs L55 — options.DetailedErrors = true exposes full stack traces to clients. Fix: wrap in if (env.IsDevelopment()).
Unrestricted AllowedHosts

appsettings.json — "AllowedHosts": "*" allows Host header injection. Fix: restrict to "localhost;127.0.0.1".
Phase 2: High-Priority Security & Resource Fixes
No Brute Force Protection on Login — Login.razor L75-79 — CheckPassword() has no rate-limiting. Add exponential backoff or lockout after N failures.

SHA256 Password Hashing Without Salt — AppSettings.cs L553 — Use Rfc2898DeriveBytes (PBKDF2) with a random salt instead of raw SHA256.

Browser Objects Never Disposed — MainWindow.xaml.cs — ChromiumWebBrowser and WebView2 created but never disposed → process leaks. Implement IDisposable and dispose in OnClosed.

SemaphoreSlim Resource Leak — Web.cs L43-73 — Static SemaphoreSlim is recreated on each call without disposing the previous instance. Convert to local using-scoped variable.

Blocking .Result Anti-Pattern — Files.cs L296, L313, L330 — HClient.GetStringAsync(uri).Result can deadlock or freeze the UI. Convert to proper async/await.

Unsanitized Process Arguments in Updater — MainWindow.xaml.cs L257-265 — Arguments passed to runas elevated process without validation. Escape/validate arguments.

Missing Security Headers — Startup.cs — No CSP, X-Frame-Options, or X-Content-Type-Options headers. Add middleware to set these.

Hardcoded Discord Client ID — AppData.cs L112 — Move DiscordRpcClient("973188269405765682") to appsettings.json.

Phase 3: Code Quality & Real-World Enhancements
Expand Test Coverage — Currently only 5 theory tests in SteamTests.cs covering Steam ID validation. Add tests for file operations, settings serialization, process management, and updater logic.

Eliminate Silent catch (Exception) { } Blocks — 20+ instances across the codebase silently swallowing errors (e.g., Startup.cs L134, SteamSwitcherFuncs.cs L741-765). Add structured logging to every catch.

Async/Await Modernization — Replace new Thread(...).Start() (AppData.cs L73) with Task.Run(). Convert all .Result blocking calls to async/await chains.

Add HTTP Request Timeouts — AppStats.cs L195 — httpClient.PostAsync(...).Result with no timeout can hang indefinitely. Set HttpClient.Timeout.

Replace XmlDocument with XDocument — Legacy XmlDocument in Steam functions → use LINQ-to-XML (XDocument), which is safer by default and has a cleaner API.

Deduplicate Shared Code — ReadAllText / ReadAllLines are duplicated in the Updater vs Globals. Extract to the shared Globals library.

Add Vulnerability Scanning to CI — appveyor.yml has no security checks. Add dotnet list package --vulnerable and consider CodeQL/Roslyn analyzers.

Structured Logging — The codebase mixes Logger.WriteLine(), WriteToLog(), and Console.WriteLine(). Adopt Serilog or the built-in ILogger<T> with proper log levels.

Secure CI/CD Pipeline — Use SFTP instead of FTP for artifact upload. Pin npm dependencies. Add post-upload hash verification.

Relevant Files
File	What to modify
SteamSwitcherFuncs.cs	Fix XXE, replace XmlDocument
GeneralInvocableFuncs.cs	Fix path traversal
Processes.cs	Fix command injection
Startup.cs	DetailedErrors, security headers, CSRF
appsettings.json	AllowedHosts, Discord client ID
Login.razor	Rate-limiting
AppSettings.cs	Salted password hashing
MainWindow.xaml.cs	Browser disposal
Web.cs	SemaphoreSlim fix
Files.cs	Async conversion
MainWindow.xaml.cs	Argument sanitization
AppData.cs	Move hardcoded ID to config
AppStats.cs	HTTP timeouts
SteamTests.cs	Expand test suite
appveyor.yml	CI security scanning
Verification
dotnet build TcNo-Acc-Switcher-Server/TcNo-Acc-Switcher-Server.sln — no regressions
dotnet test — existing tests still pass
dotnet list package --vulnerable — no known vulnerable packages
Manual test: login with wrong password → verify rate-limiting works
Manual test: Steam profile fetch → verify XML parsing doesn't accept DTDs
Manual test: close client window → verify no orphaned browser processes
Decisions
Scope is limited to fixes implementable today without major architectural rewrites
Newtonsoft.Json → System.Text.Json migration is optional (large scope, low urgency since 13.0.4 is current)
Cross-platform support is not in scope (Windows-only app by design)
Phase 1 items are security-critical and should be done first; Phase 2 and 3 can be parallelized
