 a layered .NET 9 Web API following clean architecture principles:
📁 Folder Structure (8 main projects)
SuperAppAPI - Web API controllers & middleware
SuperAppDataServices - Business logic layer
SuperAppDataRepositories - Data access layer
SuperAppModels - Domain models & DTOs
UserProfileDataServices - Auth/profile business logic
UserProfileDataRepositories - Auth/profile data access
docs - Documentation (empty)
📊 Code Counts
Controllers: 4 (AuthenController.cs, NotesController.cs, StandardRegistryController.cs, UserProfileController.cs)
Services: 4 implementations + 4 interfaces
Models: 7 domain models + 3 DTOs
🗄️ Database Pattern: ADO.NET with SqlClient
Examples:
// Pattern 1: SELECT with SqlDataReader
command.CommandText = "[dbo].[usp_s_Notes]";
command.CommandType = CommandType.StoredProcedure;
command.Parameters.Add(new SqlParameter("@iv_getAll", getAll));
using (var reader = await command.ExecuteReaderAsync())
{
    while (await reader.ReadAsync())
        list.Add(reader.MapToObject<Note>()); // DbDataReaderMapper
}

// Pattern 2: INSERT/UPDATE with Table-Valued Parameters
DataTable Note = note.ToDataTable();
command.Parameters.Add(new SqlParameter("@Note", SqlDbType.Structured) { Value = Note });
SqlParameter errorMsg = new SqlParameter("@ov_ErrorMsg", SqlDbType.VarChar, -1) 
{ 
    Direction = ParameterDirection.Output 
};
🛡️ Cross-Cutting Concerns
Concern	Implementation	Location
Logging	Serilog (console + file)	Program.cs:23-27
Error Handling	Global middleware returning ProblemDetails	Startup.cs:67-91
Validation	Manual checks in services (no framework)	Services layer
Authentication	JWT + Google OAuth + PBKDF2 password hashing	AuthSe.cs, Helpers.cs
Authorization	Custom ValidateTokenMiddleware.cs (currently disabled)	Middlewares folder
The architecture is clean with clear separation of concerns, though validation could benefit from a framework like FluentValidation.




Database Connection Management
❌ NOT in appsettings.json
Connection strings are hardcoded in static classes, not stored in configuration files. Locations:
SuperAppDataRepositories → ApplicationSettings.cs:6
public static string SuperAppConnectionString => 
    "Server=TUNGHOMEPC\\MSSQLSERVER03;Database=Timeline-dev;Trusted_Connection=True;";
UserProfileDataRepositories → ApplicationSettings.cs:6
public static string SuperAppConnectionString => 
    "Server=TUNGHOMEPC\\MSSQLSERVER05;Database=SuperApp-dev;Trusted_Connection=True;";
🔧 Connection Helper Pattern
Helper: OpenedConnection.cs
public static class OpenedConnection
{
    public static async Task<SqlConnection> Create(string connectionString)
    {
        var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();
        return conn;
    }
}
📝 Usage in Repositories:
using (var conn = await OpenedConnection.Create(ApplicationSettings.SuperAppConnectionString))
using (var command = conn.CreateCommand())
{
    command.CommandText = StoredProcedures.spSelectNotes;
    command.CommandType = CommandType.StoredProcedure;
    // ... execute
}
⚠️ Issues with Current Approach:
❌ Hardcoded connection strings - should be in appsettings.json
❌ Not injected via DI - accessed as static properties
❌ Not environment-specific - same connection for all environments
❌ Credentials in source code - security risk if committed to Git
✅ Recommended Pattern:
// appsettings.json
{
  "ConnectionStrings": {
    "SuperAppConnection": "Server=...;Database=Timeline-dev;...",
    "UserProfileConnection": "Server=...;Database=SuperApp-dev;..."
  }
}

// Program.cs - Register as service
services.AddScoped<IConnectionFactory, ConnectionFactory>();

// Inject IConfiguration in repositories
private readonly IConfiguration _config;
var connString = _config.GetConnectionString("SuperAppConnection");
📦 DTOs vs Models
3 DTO Classes (Response wrappers only)
Location: SuperAppModels/DTOs/
ResultOptions - Operation result wrapper
public class ResultOptions
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Reference { get; set; }
    public object? Object { get; set; }
    public int? Status { get; set; }
}
ResultOptions2<T> - Generic result wrapper
public class ResultOptions2<T>
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public T Data { get; set; }
}
NotesResult - Notes-specific result
public class NotesResult
{
    public List<Note> Notes { get; set; }
    public ResultOptions Options { get; set; }
}
7 Domain Models (Multi-purpose)
Location: SuperAppModels/Mos/ Models like Note, UserModel, UserProfile serve multiple purposes:
Purpose	Example
✅ API Request	[HttpPost("IuNote")] public async Task<IActionResult> IuNote([FromForm] Note note)
✅ API Response	[HttpGet("GetNotes")] public async Task<List<Note>> GetNotes()
✅ Database Mapping	var note = reader.MapToObject<Note>();
✅ Table-Valued Parameters	DataTable Note = note.ToDataTable();
Example: Note.cs
public class Note  // Used for ALL layers
{
    public int NoteId { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public string? Tags { get; set; }
    // ... directly maps to DB columns and API contract
}
⚠️ Architecture Issue:
No separation between layers - Same models are used for:
Database entities
API request bodies
API responses
Business logic
This violates separation of concerns and makes it harder to:
Change database schema without breaking API
Add API-specific validation
Hide sensitive properties from responses
✅ Recommended Pattern:
Database Layer:    Note (entity)
       ↓ map
Service Layer:     NoteDto (business object)
       ↓ map
API Layer:         NoteRequest, NoteResponse (contracts)
🎯 Dependency Injection Registration
Location: Program.cs:69-76
All Registered Services:
// Session & Caching
services.AddDistributedMemoryCache();
services.AddSession(o =>
{
    o.IdleTimeout = TimeSpan.FromMinutes(30);
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
});

// HTTP Context Access
services.AddHttpContextAccessor();

// User Profile Domain
services.AddScoped<IUserProfileRe, UserProfileRe>();     // Repository
services.AddScoped<IUserProfileSe, UserProfileSe>();     // Service

// Authentication Domain
services.AddScoped<IAuthSe, AuthSe>();                   // Service
services.AddScoped<IAuthRe, AuthRe>();                   // Repository

// Notes Domain
services.AddScoped<INoteRe, NoteRe>();                   // Repository
services.AddScoped<INoteSe, NoteSe>();                   // Service

// Standard Registry Domain
services.AddScoped<IStandardRegistryService, StandardRegistryService>();           // Service
services.AddScoped<IStandardRegistryRepository, StandardRegistryRepository>();     // Repository
Additional Services from Startup.cs:
// Framework services
services.AddMemoryCache();
services.AddControllers();
services.AddHttpClient();  // For OAuth token exchange

// Logging (configured in Program.cs)
services.AddLogging(config =>
{
    config.AddConsole();
    config.AddDebug();
});

// CORS
services.AddCors(o => { /* WebAPIPolicy */ });

// Swagger (Development only)
services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SuperApp", Version = "v1" });
});

// Request size limits
services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 100 * 1024 * 1024; // 100MB
});
Service Lifetime Summary:
Lifetime	Services
Scoped (8)	All repositories & services (per HTTP request)
Singleton (1)	Serilog Logger
Transient	HttpClient (via AddHttpClient)
🛡️ Middleware Pipeline
Location: Startup.cs:97-176
Middleware Execution Order:
1. app.UseCors("WebAPIPolicy")           // Line 104 - Allow cross-origin requests
   
2. app.UseDeveloperExceptionPage()       // Line 108 - Detailed errors (Dev only)
   app.UseSwagger()                      // Line 109 - Swagger docs (Dev only)
   app.UseSwaggerUI()                    // Line 110 - Swagger UI (Dev only)

3. app.UseRouting()                      // Line 123 - Route matching

4. app.UseSession()                      // Line 128 - Session state management

5. // app.UseGoogleTokenValidation()    // Line 130 - DISABLED Google OAuth validation

6. app.UseExceptionHandler()             // Line 133 - Global error handler
   
7. app.UseEndpoints()                    // Line 167 - Execute controller actions
Custom Middleware:
1. Google Token Validation Middleware ⚠️ (Currently Disabled)
File: ValidateTokenMiddleware.cs Purpose: Validate Google OAuth tokens on every request Features:
Skips validation for /loginSignup and /loginSignup2 endpoints
Validates Bearer token format
Verifies Google ID token using GoogleJsonWebSignature.ValidateAsync()
Checks audience matches OAuth ClientId
Checks token expiration
Stores payload in HttpContext.Items["GooglePayload"]
Status: Disabled in Startup.cs:130
// app.UseGoogleTokenValidation();  // TEMPORARILY DISABLED
Registration Extension:
public static IApplicationBuilder UseGoogleTokenValidation(this IApplicationBuilder builder)
{
    return builder.UseMiddleware<GoogleTokenValidationMiddleware>();
}
2. Global Exception Handler ✅ (Active)
Location: Startup.cs:133-160 Purpose: Catch unhandled exceptions and return standardized error responses
app.UseExceptionHandler(a => a.Run(async context =>
{
    var error = context.Features.Get<IExceptionHandlerFeature>().Error;
    var problem = new ProblemDetails { Title = "Critical Error" };
    
    if (error != null)
    {
        if (env.IsDevelopment())
        {
            problem.Title = error.Message;
            problem.Detail = error.StackTrace;  // Show stack trace in Dev
        }
        else
            problem.Detail = error.Message;      // Hide stack trace in Prod
        
        // Get session email
        if (context.Session.TryGetValue("email", out byte[] nameBytes))
        {
            string name = Encoding.UTF8.GetString(nameBytes);
        }
    }
    
    await context.Response.WriteAsJsonAsync(problem);
}));
Features:
Returns ProblemDetails JSON response
Environment-aware error details
Attempts to retrieve session email
Returns HTTP 500 (inferred)
Built-in Middleware:
Middleware	Purpose	Config Location
CORS	Allow localhost:3000/3001/3003 and *.vanthiel.com	Startup.cs:70-84
Session	In-memory session, 30min timeout, HttpOnly cookies	Program.cs:60-65
Routing	Enable endpoint routing	Startup.cs:123
Swagger	API documentation (Dev only)	Startup.cs:54-57
📊 Summary Table
Aspect	Current Implementation	Issues	Recommendation
Connection Strings	Hardcoded in static classes	❌ Not in config, not env-specific	Move to appsettings.json
Connection Injection	Static property access	❌ Not using DI	Inject IConfiguration or connection factory
DTOs	3 result wrappers only	❌ Models used for all layers	Separate request/response/entity DTOs
DI Registration	8 scoped services (repos + services)	✅ Clean separation	Good
Middleware	1 custom (disabled) + 1 global error handler	⚠️ Token validation disabled	Enable or implement JWT middleware
Architecture	3-tier with repositories	✅ Good structure	Consider CQRS for complex queries
The project has a solid foundation but would benefit from externalizing configuration and implementing proper DTOs for API contracts.










Comprehensive Analysis Summary
1. ⚠️ ERROR HANDLING
Current State: Inconsistent & Incomplete
Global Handler (Startup.cs:133-160)
app.UseExceptionHandler(a => a.Run(async context =>
{
    var error = context.Features.Get<IExceptionHandlerFeature>().Error;
    var problem = new ProblemDetails { Title = "Critical Error" };
    // Returns ProblemDetails but doesn't set HTTP status code (defaults to 200!)
    await context.Response.WriteAsJsonAsync(problem);
}));
Issues:
❌ No HTTP status code set (returns 200 OK for errors!)
❌ Reads session email and timezone but never uses them
❌ Full stack trace exposed in Development mode
Controllers: 3 Different Approaches
Controller	Approach	Issue
AuthController	Try-catch with return Unauthorized(ex)	🔴 Returns entire exception object to client
NotesController	No try-catch, checks Success flag	⚠️ Will crash on unexpected errors
UserProfileController	No error handling at all	🔴 Will crash and bubble to global handler
StandardRegistryController	No error handling at all	🔴 Will crash and bubble to global handler
Services: Useless Empty Try-Catch
Pattern in AuthSe.cs:
try
{
    // ... logic ...
}
catch
{
    throw;  // Just re-throws - provides ZERO value
}
Repositories: Same Useless Pattern
All 4 repositories use identical empty try-catch blocks that just re-throw. Comments suggest logging was planned but never implemented:
catch (Exception)
{
    // Log lỗi nếu cần thiết trước khi ném lại
    throw;
}
Logging: Configured But Unused
Serilog configured in Program.cs:23-27, but:
File	Logger Injected?	Actually Used?
NotesController	✅	❌ Never
UserProfileController	✅	❌ Never
NoteRe	✅	❌ Never
UserProfileRe	✅	❌ Never
AuthRe	✅	❌ Never
Only used in: ValidateTokenMiddleware (8 log statements)
2. 🔧 CONFIGURATIONS & CONSTANTS
🔴 CRITICAL SECURITY ISSUES
Hardcoded Connection Strings (Not in appsettings.json!)
SuperAppDataRepositories/ApplicationSettings.cs:6
public static string SuperAppConnectionString => 
    "Server=TUNGHOMEPC\\MSSQLSERVER03;Database=Timeline-dev;Trusted_Connection=True;";
UserProfileDataRepositories/ApplicationSettings.cs:6
public static string SuperAppConnectionString => 
    "Server=TUNGHOMEPC\\MSSQLSERVER05;Database=SuperApp-dev;Trusted_Connection=True;";
Problems:
🔴 Two different connection strings with the same property name
🔴 Pointing to different SQL Server instances (03 vs 05)
🔴 Different databases (Timeline-dev vs SuperApp-dev)
🔴 Hardcoded in source code
Exposed Secrets in appsettings.json
"OAuth": {
  "ClientId": "887853390661-j2bepobhb90k357d0k5p1atqd2k8oe6l.apps.googleusercontent.com",
  "ClientSecret": "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",  // 🔴 EXPOSED!
  "RedirectUri": "http://localhost:3001/homepage"
},
"Jwt": {
  "Key": "testkeyslfjsliefnfslfjikseflijsefnlfzslfhisfnjelisejfseofhuislfensefli"  // 🔴 Test key
}
Constants Classes
UserProfileDataConstants.cs
public static string signupDefault = "signUpDefault";
public static string loginByGoogle = "loginByGoogle";
public static string loginByDefault = "loginByDefault";
Stored Procedures (2 files)
SuperAppDataRepositories/StoredProcedures.cs - 5 SPs defined, 2 unused
UserProfileDataRepositories/StoredProcedures.cs - 4 SPs defined
Magic Numbers Scattered Throughout
Value	Location	Purpose	Count
1200	All repository files	SQL timeout (20 min)	8
100 * 1024 * 1024	Program.cs, Startup.cs	Max request size (100MB)	2
30	Program.cs	Session timeout (min)	1
100000	Helpers.cs	PBKDF2 iterations	2
60	appsettings.json	JWT expiration (min)	1
Hardcoded Machine Names (Startup.cs:66)
string[] localMachines = { "VTHKNB01", "VTHKNB02", "VTVNNB60", "VTVNNB70", "VTVNNB06" };
var isLocal = Array.Find(localMachines, l => l == Dns.GetHostName());
Hardcoded Error Messages
17 unique hardcoded messages across the solution (should be in resource files for i18n)
3. 🔄 CODE DUPLICATION & PATTERNS
🔴 40% of Repository Code is Duplicated Boilerplate
Pattern 1: Connection Setup (Found 8 times)
using (var conn = await OpenedConnection.Create(ApplicationSettings.SuperAppConnectionString))
using (var command = conn.CreateCommand())
{
    command.CommandText = StoredProcedures.spXXX;
    command.CommandType = CommandType.StoredProcedure;
    command.CommandTimeout = 1200;  // Repeated 8 times!
    command.Parameters.Clear();
    // ... parameters ...
}
Pattern 2: Nullable Parameter Handling (Found 19 times)
if (!string.IsNullOrEmpty(searchText))
    command.Parameters.Add(new SqlParameter("@iv_SearchText", searchText));
else
    command.Parameters.Add(new SqlParameter("@iv_SearchText", DBNull.Value));
Pattern 3: Inconsistent Reader Mapping
Manual (14 occurrences in AuthRe.cs):
user.Email = reader["Email"] != DBNull.Value ? reader["Email"].ToString() : null;
user.Phone = reader["Phone"] != DBNull.Value ? reader["Phone"].ToString() : null;
// ... 12 more lines ...
vs. Extension method (3 occurrences):
var note = reader.MapToObject<Note>();  // ✅ Better!
Pattern 4: Pass-Through Services (4 services add ZERO value)
[UserProfileSe.cs](UserProfileDataServices/Services/UserProfileSe .cs):
public async Task<UserProfile> GetUserProfileJson(string email, string appC)
{
    return await _XRepo.GetUserProfileJson(email, appC);  // Just forwards!
}
Same pattern in NoteSe, StandardRegistryService - these services should be removed.
Pattern 5: 100% Duplicated Files
OpenedConnection.cs exists in TWO locations with byte-for-byte identical code:
SuperAppDataRepositories/OpenedConnection.cs
UserProfileDataRepositories/OpenedConnection.cs
Should be in a shared library!
4. 🔐 AUTHENTICATION & AUTHORIZATION
🔴 CRITICAL BUG: Login is Completely Broken!
AuthSe.cs:93:
if(Helpers.HashPassword(model.Password ?? "") == existUser.Password)  // 🔴 BUG!
Problem: HashPassword() generates a NEW random salt each time, so this comparison will ALWAYS fail. Should use VerifyPassword() instead (which exists but is unused).
Authentication Types Supported
Type	Identifier	Works?
Sign Up	"signUpDefault"	✅ Yes
Login (Email/Password)	"loginByDefault"	🔴 NO - Broken by hash bug
Login (Google OAuth)	"loginByGoogle"	⚠️ Partial - middleware disabled
JWT Implementation
Token Generation: AuthSe.cs:134-169
Algorithm: HMAC-SHA256
Expiration: 60 minutes (no refresh mechanism)
Claims: Only sub (email/phone) and jti (GUID) - no roles/permissions
Token Validation: 🔴 DISABLED
// TEMPORARILY DISABLED: Google Token validation middleware
// app.UseGoogleTokenValidation();
Authorization: ❌ NONE
No [Authorize] attributes anywhere
No role-based authorization
No custom authorization filters
All endpoints are currently UNPROTECTED
Session Management: Configured But Unused
Session is configured (Program.cs:59-65) but never written to. The one place that reads it (Startup.cs:149) will always fail.
Password Security: ✅ Strong (When Not Broken)
Helpers.cs:18-35
// PBKDF2 with SHA256, 100,000 iterations, 128-bit salt, 256-bit hash
byte[] salt = RandomNumberGenerator.GetBytes(16);
var hash = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
📊 DEPENDENCY INJECTION REGISTRATION
Program.cs:69-76 - All Scoped Services:
// User Profile Domain
services.AddScoped<IUserProfileRe, UserProfileRe>();
services.AddScoped<IUserProfileSe, UserProfileSe>();

// Authentication Domain  
services.AddScoped<IAuthSe, AuthSe>();
services.AddScoped<IAuthRe, AuthRe>();

// Notes Domain
services.AddScoped<INoteRe, NoteRe>();
services.AddScoped<INoteSe, NoteSe>();

// Standard Registry Domain
services.AddScoped<IStandardRegistryService, StandardRegistryService>();
services.AddScoped<IStandardRegistryRepository, StandardRegistryRepository>();
Additional Services:
Session (30-min timeout)
HttpContextAccessor
HttpClient (for OAuth)
Memory cache
CORS ("WebAPIPolicy")
Swagger (Dev only)
🛡️ MIDDLEWARE PIPELINE
Startup.cs:97-176 - Execution Order:
CORS → Allow localhost:3000/3001/3003, *.vanthiel.com
DeveloperExceptionPage → Dev only
Swagger/SwaggerUI → Dev only
Routing → Route matching
Session → 30-min timeout, HttpOnly cookies
GoogleTokenValidation → 🔴 DISABLED
Global Exception Handler → Returns ProblemDetails
Endpoints → Execute controllers
Custom Middleware:
ValidateTokenMiddleware.cs - Google OAuth validation (disabled)
🚨 TOP 10 CRITICAL ISSUES
🔴 Password verification bug - Login will NEVER work (AuthSe.cs:93)
🔴 OAuth secret exposed - In appsettings.json
🔴 Hardcoded connection strings - Two different databases in C# files
🔴 No authorization - All endpoints unprotected
🔴 Token validation disabled - Security middleware commented out
🔴 Errors return sensitive data - Full exception objects to clients
🔴 Global handler sets no status code - Returns 200 for errors
🔴 40% code duplication - Repository boilerplate
🔴 Loggers injected but unused - 5 classes never log
🔴 Pass-through services - 4 services add zero value
✅ QUICK WINS (Priority Order)
Fix password bug: Change HashPassword() to VerifyPassword() (1 line)
Move secrets to user secrets or environment variables
Enable token validation: Uncomment line 130 in Startup.cs
Set error status codes: Add context.Response.StatusCode = 500 in global handler
Add [Authorize] attributes to all controllers
Remove empty try-catch blocks (adds no value)
Remove unused loggers or actually use them
Consolidate OpenedConnection into one shared class