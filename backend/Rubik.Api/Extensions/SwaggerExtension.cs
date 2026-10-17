using System.Text.Json;
using Rubik.Api.Configuration;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Rubik.Api.Extensions;

public static class SwaggerExtension
{
    /// <summary>
    /// Adds a UI button to the Swagger UI top bar that opens the React app. The URL comes from
    /// <see cref="IRubikConfiguration"/> ("links:ui", defaulting to the Vite dev server). Add more
    /// [label, url] pairs to the list to get more buttons.
    /// </summary>
    public static void AddTopBarLinks(SwaggerUIOptions options, IRubikConfiguration config)
    {
        var links = JsonSerializer.Serialize(new[]
        {
            new[] { RubikConstants.Link.UiLabel, config.UiUrl },
        });

        options.HeadContent += $$"""
            <style>
              /* Scoped under .swagger-ui .topbar to outrank Swagger UI's own
                 `.swagger-ui nav { display: block }` and `.swagger-ui .topbar a` rules. */
              .swagger-ui .topbar .rubik-links {
                display: flex; flex-direction: row; flex: 0 0 auto; gap: 8px;
                margin-left: 24px; align-items: center;
              }
              .swagger-ui .topbar .rubik-links a {
                flex: 0 0 auto; max-width: none;
                color: #fff; font-weight: 600; font-size: 14px; text-decoration: none;
                padding: 6px 14px; border: 2px solid #62a03f; border-radius: 4px;
              }
              .swagger-ui .topbar .rubik-links a:hover { background: #62a03f; }
            </style>
            <script>
              window.addEventListener('load', function () {
                var links = {{links}};
                var timer = setInterval(function () {
                  var bar = document.querySelector('.topbar-wrapper');
                  if (!bar) return;
                  clearInterval(timer);
                  var nav = document.createElement('nav');
                  nav.className = 'rubik-links';
                  links.forEach(function (link) {
                    var a = document.createElement('a');
                    a.textContent = link[0];
                    a.href = link[1];
                    a.target = '_blank';
                    a.rel = 'noopener';
                    nav.appendChild(a);
                  });
                  bar.insertBefore(nav, bar.querySelector('.download-url-wrapper'));
                }, 50);
              });
            </script>
            """;
    }
}
