# Petit client CDP (Chrome DevTools Protocol) pour observer DRAFT dans sa vraie WebView2.
# Usage : . .\docs\ux\outils\cdp.ps1 ; puis Get-CdpTargets -Port 9339, Invoke-CdpEval, Save-CdpShot.
# Le port n'écoute que sur 127.0.0.1 et seulement pour un DRAFT lancé par audit-webview2.ps1, avec un dossier de données temporaire.
# Aucune donnée personnelle : l'application testée part d'un dossier vide.
$script:CdpId = 0

function Get-CdpTargets([int]$Port = 9339) {
  (Invoke-WebRequest -UseBasicParsing "http://127.0.0.1:$Port/json/list").Content | ConvertFrom-Json | Where-Object { $_.type -eq 'page' }
}

function Invoke-Cdp($Target, [string]$Method, $Params = @{}) {
  $ws = [System.Net.WebSockets.ClientWebSocket]::new()
  $ws.Options.KeepAliveInterval = [TimeSpan]::FromSeconds(30)
  $ws.ConnectAsync([Uri]$Target.webSocketDebuggerUrl, [Threading.CancellationToken]::None).Wait()
  try {
    $script:CdpId++
    $id = $script:CdpId
    $msg = @{ id = $id; method = $Method; params = $Params } | ConvertTo-Json -Depth 10 -Compress
    $bytes = [Text.Encoding]::UTF8.GetBytes($msg)
    $ws.SendAsync([ArraySegment[byte]]::new($bytes), 'Text', $true, [Threading.CancellationToken]::None).Wait()
    $buf = [byte[]]::new(1048576)
    while ($true) {
      $sb = [Text.StringBuilder]::new()
      do {
        $r = $ws.ReceiveAsync([ArraySegment[byte]]::new($buf), [Threading.CancellationToken]::None).Result
        [void]$sb.Append([Text.Encoding]::UTF8.GetString($buf, 0, $r.Count))
      } while (-not $r.EndOfMessage)
      $o = $sb.ToString() | ConvertFrom-Json -Depth 50
      if ($o.id -eq $id) { return $o }
    }
  } finally { try { $ws.CloseAsync('NormalClosure', '', [Threading.CancellationToken]::None).Wait() } catch {} ; $ws.Dispose() }
}

function Invoke-CdpEval($Target, [string]$Expr) {
  $r = Invoke-Cdp $Target 'Runtime.evaluate' @{ expression = $Expr; returnByValue = $true; awaitPromise = $true }
  if ($r.result.exceptionDetails) { return 'EXCEPTION: ' + ($r.result.exceptionDetails | ConvertTo-Json -Depth 5 -Compress) }
  $r.result.result.value
}

function Save-CdpShot($Target, [string]$File) {
  $r = Invoke-Cdp $Target 'Page.captureScreenshot' @{ format = 'png' }
  [IO.File]::WriteAllBytes($File, [Convert]::FromBase64String($r.result.data))
}
