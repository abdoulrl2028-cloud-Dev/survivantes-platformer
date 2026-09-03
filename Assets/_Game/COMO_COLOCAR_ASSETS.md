# COMO COLOCAR ASSETS REAIS NO JOGO (Operation Dustline)

Este projeto está pronto para receber assets reais. Como as ferramentas de download
automático (Sketchfab/Mixamo) não têm chave de API configurada nesta máquina, o caminho
é: você baixa os arquivos, coloca nas pastas certas, e eu **importo e conecto tudo** no
cenário (via import_model_file) + subo o lighting realista.

## Onde colocar cada arquivo

### 1) BACKGROUND / PLATAFORMA — do site Sketchfab
- No Sketchfab, baixe o modelo como **.glb** (ou .fbx/.obj) com texturas.
- Coloque em:  `Assets/_Game/Art/Environment/`
  (ex.: `Assets/_Game/Art/Environment/meu_ambiente.glb`)
- Se for só um cenário de fundo/plataforma, pode ser também `.hdr` para o céu
  (colocar em `Assets/_Game/Audio/Environment`? Não — em `Assets/_Game/Art/Environment/`).

### 2) INIMIGOS — do site Mixamo
- No Mixamo, baixe o personagem em **.fbx + com a animação** (ex.: "Walking",
  "Running", "Idle", "Shooting", "Death"). Escolha "With Skin" para o raster de modelo.
- Coloque em:  `Assets/_Game/Art/Characters/`
  (ex.: `Assets/_Game/Art/Characters/inimigo.fbx` + `inimigo_idle.fbx`, etc.)

### 3) PLAYER — também do Mixamo
- Baixe do mesmo jeito (personagem + animações em .fbx).
- Coloque em:  `Assets/_Game/Art/Characters/`
  (ex.: `Assets/_Game/Art/Characters/player.fbx` + `player_run.fbx`, etc.)

### 4) AMBIENTE (realista e bonito)
- Modelos de ambiente (construções, vegetação, estradas) do Sketchfab/Polyhaven:
  `Assets/_Game/Art/Environment/`
- Texturas PBR (Polyhaven): `Assets/_Game/Art/Materials/`
- HDRI de céu (Polyhaven, .hdr): `Assets/_Game/Art/Environment/`
- Sons: `Assets/_Game/Audio/...`

## Formatos aceitos
- Fbx / obj / glb / gltf / zip (contendo o modelo + texturas).
- HDRI (`.hdr`) para o céu.

## Quando você colocar os arquivos
Avisa-me (ex.: "coloquei o ambiente e os inimigos"). Aí eu:
1. Importo tudo com `import_model_file` (não precisa chave de API).
2. Substituo os placeholders (primitivos) do cenário pelos modelos reais.
3. Configuro o Animator do Inimigo/Player com as animações Mixamo.
4. Melhoro lighting + post-processing + skybox para um visual realista.
5. Commito e publico no GitHub.

## Nota
- O jogador ainda funciona com WASD/mouse (FirstPersonController) — os modelos
  entrarão para o visual e as animações; não é preciso virar VR (você escolheu PC).
