$files = @("EarthParticleRenderer.cs", "LifeParticleRenderer.cs", "MagicParticleRenderer.cs", "TechParticleRenderer.cs", "UndeadParticleRenderer.cs")

$helperMethod = @'

        private float GetXPositionForSide(ParticleSide side, double canvasWidth)
        {
            return side switch
            {
                ParticleSide.Left => (float)(Random.Shared.NextDouble() * canvasWidth * 0.5),
                ParticleSide.Right => (float)(Random.Shared.NextDouble() * canvasWidth * 0.5 + canvasWidth * 0.5),
                _ => (float)(Random.Shared.NextDouble() * canvasWidth)
            };
        }
'@

foreach ($file in $files) {
    Write-Host "Processing $file..."
    $content = Get-Content $file -Raw
    
    # Update EmitParticles signature
    $content = $content -replace '(public void EmitParticles\(List<Particle> particles, Size canvasSize, float deltaTime)\)', '$1, ParticleSide side)'
    
    # Add helper method before the last closing braces
    $content = $content -replace '(\s+)\}\s+\}\s*$', "$1$helperMethod`n`$1}`n}"
    
    Set-Content $file -Value $content -NoNewline
    Write-Host "  Updated $file"
}
