#region Usings

using Elm.API.Handler;
using Elm.Application;
using Elm.Application.Contracts.Abstractions.Cache;
using Elm.Application.Contracts.Abstractions.Excel;
using Elm.Application.Contracts.Abstractions.Files;
using Elm.Application.Contracts.Abstractions.Realtime;
using Elm.Application.Contracts.Abstractions.Settings;
using Elm.Application.Contracts.Abstractions.TestService;
using Elm.Application.Contracts.Const;
using Elm.Application.Contracts.Repositories;
using Elm.Application.Helper;
using Elm.Application.Mapper.Elm.Application.Mappers;
using Elm.Domain.Entities;
using Elm.Infrastructure;
using Elm.Infrastructure.BackgroundServices;
using Elm.Infrastructure.Notifications;
using Elm.Infrastructure.Repositories;
using Elm.Infrastructure.Services.Cache;
using Elm.Infrastructure.Services.Excel;
using Elm.Infrastructure.Services.Files;
using Elm.Infrastructure.Services.Realtime;
using Elm.Infrastructure.Services.Settings;
using Elm.Infrastructure.Services.TestService;
using Exceptionless;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

#endregion

namespace ElmAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            // Add services to the container.
            //************************************************
            //====== ( Add Identity & DbContext ) ========
            builder.Services.AddIdentity<AppUser, Role>().AddEntityFrameworkStores<AppDbContext>();


            builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                builder.Configuration.GetConnectionString("DefaultConnection"),
                sqlOptions => sqlOptions.EnableRetryOnFailure() // إضافة خاصية إعادة المحاولة
            ));

            //************************************************
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(builder.Configuration)
                .CreateLogger();

            // ===== ( Serilog Configuration ) ======
            builder.Host.UseSerilog();
            // ===== ( Exceptionless Configuration ) ======
            builder.Services.AddExceptionless(builder.Configuration);

            // ===== ( ) ======
            builder.Services.Configure<SettingsOptions>(builder.Configuration.GetSection("SettingsOptions"));
            builder.Services.Configure<JWT>(builder.Configuration.GetSection("JwtSettings"));
            builder.Services.AddApplication();

            #region Repositories 

            builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            builder.Services.AddScoped<IUserPerissionRepsitory, UserPerissionRepsitory>();
            builder.Services.AddScoped<IRolePermissionRepository, RolePermissionRepsitory>();
            builder.Services.AddScoped<IUniversityRepository, UniversityRepository>();
            builder.Services.AddScoped<ICollegeRepository, CollegeRepository>();
            builder.Services.AddScoped<IYearRepository, YearRepository>();
            builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
            builder.Services.AddScoped<ISubjectRepository, SubjectRepository>();
            builder.Services.AddScoped<ICurriculumRepository, CurriculumRepository>();
            builder.Services.AddScoped<IQuestionBankRepository, QuestionBankRepository>();
            builder.Services.AddScoped<IQuestionRepository, QuestionRepository>();
            builder.Services.AddScoped<IFileStorageService, FileStorageService>();
            builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
            builder.Services.AddScoped<IDoctorRepository, DoctorRepository>();

            builder.Services.AddScoped<IStudentRepository, StudentRepository>();

            #endregion

            #region Infrastructure Services

            builder.Services.AddHostedService<NotificationCleanupService>();
            builder.Services.AddHostedService<RefreshTokenCleanupService>();
            builder.Services.AddScoped<IExcelWriter, ExcelWriter>();
            builder.Services.AddScoped<IExcelReader, ExcelReader>();
            builder.Services.AddScoped<INotificationService, NotificationService>();
            builder.Services.AddScoped<ITestSessionService, TestSessionService>();
            builder.Services.AddScoped<ITestScoringService, TestScoringService>();
            builder.Services.AddScoped<ISettingsService, SettingsService>();
            builder.Services.AddScoped<IGenericCacheService, GenericCacheService>();

            #endregion

            #region Mapper

            builder.Services.AddSingleton<MappingProvider>();
            //builder.Services.AddAutoMapper(
            //    //typeof(Program).Assembly, // API Assembly
            //    Assembly.Load("Elm.Application") // Core Assembly
            //);
            #endregion

            #region Global Handler Exception 

            builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
            builder.Services.AddProblemDetails();

            #endregion

            #region Policy
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("policy", policy =>
                {
                    policy.WithOrigins(
                        "http://localhost:4200",
                        "https://elm-university.netlify.app"
                    )
                       .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials()
                      .WithExposedHeaders("Retry-After");
                });
            });

            #endregion

            #region JwtBearer & Authentication

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
                .AddJwtBearer(o =>
                {
                    o.RequireHttpsMetadata = false; // for development
                    o.SaveToken = true;
                    o.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        ValidateIssuer = false,   // if false will be public
                        ValidateAudience = false, // if false will be public
                        ValidateLifetime = true,
                        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
                        ValidAudience = builder.Configuration["JwtSettings:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"]!)),
                        ClockSkew = TimeSpan.Zero
                    };
                    o.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            // 1. محاولة قراءة التوكن من الـ Query String
                            var accessToken = context.Request.Query["access_token"];

                            // 2. إذا كان التوكن موجوداً والمسار هو للـ Hub
                            var path = context.HttpContext.Request.Path;
                            if (!string.IsNullOrEmpty(accessToken) &&
                                path.StartsWithSegments("/notificationHub")) // نفس الاسم في MapHub
                            {
                                // 3. تعيين التوكن للسياق ليتم التحقق منه
                                context.Token = accessToken;
                            }
                            return Task.CompletedTask;
                        }
                    };
                });
            #endregion

            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.SameSite = SameSiteMode.None; // للسماح بالـ Cross-site
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // يجب أن يكون الموقع HTTPS
            });

            builder.Services.AddHttpContextAccessor(); // هذا السطر ضروري جداً
            builder.Services.AddAuthorization();


            builder.Services.AddSignalR();

            builder.Services.AddSingleton<IUserIdProvider, UserIdProvider>();
            builder.Services.AddMemoryCache();
            // 1. تسجيل خدمات الـ Health Check
            builder.Services.AddHealthChecks()
                .AddDbContextCheck<AppDbContext>("Database");

            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            });


            #region  Rate Limiter Configuration 

            builder.Services.AddRateLimiter(options =>
            {
                // ═══════════════════════════════════════
                //  OnRejected
                // ═══════════════════════════════════════
                options.OnRejected = async (context, token) =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    var path = context.HttpContext.Request.Path.Value ?? "";

                    TimeSpan retryAfter;
                    string message;

                    if (path.Contains("/api/Test/start", StringComparison.OrdinalIgnoreCase))
                    {
                        retryAfter = TimeSpan.FromMinutes(5);
                        message = "لقد تخطيت الحد المسموح لإنشاء الاختبارات. يرجى المحاولة بعد 5 دقائق.";
                    }
                    else if (path.Contains("/api/Auth/Login", StringComparison.OrdinalIgnoreCase))
                    {
                        retryAfter = TimeSpan.FromMinutes(3);
                        message = "لقد تخطيت الحد المسموح لتسجيل الدخول. يرجى المحاولة بعد 3 دقائق.";
                    }
                    else
                    {
                        retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var val)
                            ? val
                            : TimeSpan.FromMinutes(1);
                        message = "لقد تخطيت الحد المسموح. يرجى الانتظار قليلاً.";
                    }

                    logger.LogWarning(
                        "Rate limit. Path: {Path}, IP: {IP}, User: {User}",
                        path,
                        HelperMethod.GetClientIp(context.HttpContext),
                        context.HttpContext.User.Identity?.Name ?? "anonymous");

                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString();

                    await context.HttpContext.Response.WriteAsJsonAsync(new
                    {
                        type = "https://tools.ietf.org/html/rfc6585#section-4",
                        title = "لقد تخطيت الحد المسموح من الطلبات",
                        status = 429,
                        message,
                        retryAfterSeconds = (int)retryAfter.TotalSeconds
                    }, token);
                };

                // ═══════════════════════════════════════
                //  UserRolePolicy - للمسجلين (مع [Authorize])
                //  ✅ Partition بالـ Username = كل مستخدم مستقل
                // ═══════════════════════════════════════
                options.AddPolicy("UserRolePolicy", context =>
                {
                    var userName = context.User.Identity?.Name;
                    var userRole = context.User.FindFirstValue(ClaimTypes.Role);

                    if (string.IsNullOrEmpty(userName))
                    {
                        return RateLimitPartition.GetSlidingWindowLimiter(
                            $"unauth_{HelperMethod.GetClientIp(context)}",
                            _ => new SlidingWindowRateLimiterOptions
                            {
                                PermitLimit = 5,
                                Window = TimeSpan.FromMinutes(1),
                                SegmentsPerWindow = 6,
                                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                                QueueLimit = 0
                            });
                    }

                    var permitLimit = userRole switch
                    {
                        UserRoles.Admin => 1000,
                        UserRoles.Doctor => 300,
                        UserRoles.Leader => 400,
                        _ => 150
                    };

                    return RateLimitPartition.GetSlidingWindowLimiter(
                        $"auth_{userName}",
                        _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = permitLimit,
                            Window = TimeSpan.FromMinutes(1),
                            SegmentsPerWindow = 6,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        });
                });

                // ═══════════════════════════════════════
                //  PublicContentPolicy - للطلاب (بدون تسجيل)
                //  ✅ Partition بالـ Fingerprint = كل جهاز مستقل
                // ═══════════════════════════════════════
                options.AddPolicy("PublicContentPolicy", context =>
                {
                    var fingerprint = HelperMethod.GetDeviceFingerprint(context);

                    return RateLimitPartition.GetSlidingWindowLimiter(
                        $"public_{fingerprint}",
                        _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = 150,
                            Window = TimeSpan.FromMinutes(1),
                            SegmentsPerWindow = 6,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        });
                });

                // ═══════════════════════════════════════
                //  LoginPolicy - حماية Brute Force
                //  ✅ Partition بالـ IP (مقصود عشان نمنع الشبكة كلها)
                // ═══════════════════════════════════════
                options.AddPolicy("LoginPolicy", context =>
                {
                    var clientIp = HelperMethod.GetClientIp(context);

                    return RateLimitPartition.GetSlidingWindowLimiter(
                        $"login_{clientIp}",
                        _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = 3,
                            Window = TimeSpan.FromMinutes(3),
                            SegmentsPerWindow = 3,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        });
                });

                // ═══════════════════════════════════════
                //  TestCreationPolicy - اختبار واحد كل 5 دقائق
                //  ✅ Partition بالـ Username
                // ═══════════════════════════════════════
                options.AddPolicy("TestCreationPolicy", context =>
                {
                    var userName = context.User.Identity?.Name ?? "anonymous";

                    return RateLimitPartition.GetSlidingWindowLimiter(
                        $"test_{userName}",
                        _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = 1,
                            Window = TimeSpan.FromMinutes(5),
                            SegmentsPerWindow = 5,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        });
                });
            });
            #endregion



            builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
            builder.Services.AddProblemDetails();

            builder.Services.AddHttpContextAccessor();
            builder.Configuration
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();  // ← هذا مهم جداً!

            if (builder.Environment.IsProduction())
            {
                // في الإنتاج: استخدم مجلد ثابت
                var keysDirectory = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtection-Keys");

                if (!Directory.Exists(keysDirectory))
                {
                    Directory.CreateDirectory(keysDirectory);
                }

                builder.Services.AddDataProtection()
                    .SetApplicationName("ElmAPI")
                    .PersistKeysToFileSystem(new DirectoryInfo(keysDirectory))
                    .SetDefaultKeyLifetime(TimeSpan.FromDays(90)); // مدة صلاحية المفتاح
            }
            else
            {
                // في التطوير: الإعدادات الافتراضية كافية
                builder.Services.AddDataProtection()
                    .SetApplicationName("ElmAPI");
            }


            // all next code is written by gitHub copilot 
            // في الإنتاج، تأكد من أن Data Protection يستخدم تخزيناً ثابتاً للمفاتيح (مثل مجلد أو Azure Blob Storage)
            // في التطوير، يمكنك استخدام الإعدادات الافتراضية لـ Data Protection
            //builder.Services.AddDataProtection();
            //builder.Services.AddHttpContextAccessor();
            //builder.Services.AddProblemDetails();

            builder.Services.AddOpenApiDocument(cfg =>
            {
                cfg.Title = "ElmAPI";
                cfg.AddSecurity("JWT", new NSwag.OpenApiSecurityScheme
                {
                    Type = NSwag.OpenApiSecuritySchemeType.ApiKey,
                    Name = "Authorization",
                    In = NSwag.OpenApiSecurityApiKeyLocation.Header,
                    Description = "Bearer {token}"
                });

                // يضيف متطلبات الـ security للـ actions التي تحتوي على [Authorize] ويتجاهل [AllowAnonymous]
                cfg.OperationProcessors.Add(new NSwag.Generation.Processors.Security.AspNetCoreOperationSecurityScopeProcessor("JWT"));
            });


            // Add Scalar API Reference
            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            //if (app.Environment.IsDevelopment())
            //{
            app.MapOpenApi();
            app.MapScalarApiReference();
            //}
            //*********************************

            app.UseExceptionHandler();
            app.UseExceptionless();

            // 1. التوجيه وتأمين الاتصال
            app.UseHttpsRedirection();
            // تفعيل Static Files
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(
                    Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images")),
                RequestPath = "/Images",
                OnPrepareResponse = ctx =>
                {
                    // Cache للصور لمدة 7 أيام
                    ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=604800");

                    // Security Headers
                    ctx.Context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
                }
            });
            // 5. التسجيل (Logging)
            app.UseSerilogRequestLogging();

            app.UseRouting();
            app.UseForwardedHeaders(); // إضافة هذا السطر لمعالجة رؤوس التوجيه الأمامي
                                       // 2. السماح بالاتصال من الـ Frontend (CORS)
            app.UseCors("policy");

            // 3. الهوية والأمان (يجب أن يكونا بهذا الترتيب)
            app.UseAuthentication();
            app.UseAuthorization();


            // 4. الـ Rate Limiter (يجب أن يكون بعد الـ Authorization لكي يعرف Role المستخدم)
            app.UseRateLimiter();

            // 6. تعريف المسارات (Endpoints)
            app.MapControllers();
            app.MapHub<NotificationHub>("/notificationHub"); // تأكد من الاسم الموحد للمسار

            app.MapHealthChecks("/health", new HealthCheckOptions
            {
                ResponseWriter = async (context, report) =>
                {
                    context.Response.ContentType = "application/json";
                    var response = new
                    {
                        Status = report.Status.ToString(),
                        Checks = report.Entries.Select(e => new
                        {
                            Component = e.Key,
                            Status = e.Value.Status.ToString(),
                            Description = e.Value.Description,
                            Duration = e.Value.Duration
                        }),
                        TotalDuration = report.TotalDuration
                    };
                    await context.Response.WriteAsJsonAsync(response);
                }
            });

            app.Run();
        }
    }
}
