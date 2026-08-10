using Microsoft.Extensions.Logging;
using System;

namespace SandboxEscape;

internal readonly record struct LogEntry(LogLevel Level, string Category, string Message, Exception? Exception);
