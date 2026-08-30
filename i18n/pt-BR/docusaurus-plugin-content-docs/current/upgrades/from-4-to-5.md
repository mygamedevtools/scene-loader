---
sidebar_position: 3
title: De 4.x para 5.x
description: Atualize da versão 4.x para 5.x
---

# Atualizando da versão 4.x para 5.x

**Comece por aqui: a chamada principal não mudou.**

```cs
MySceneManager.TransitionAsync("my-target-scene", "my-loading-scene");   // 4.x e 5.x, idêntico
```

Se isso é a maior parte do que o seu projeto faz, a migração é pequena. A maioria dos projetos vai lidar com
renomeações e argumentos removidos — coisa de localizar e substituir, não de rearquitetura.

**Chamadas addressable agora ficam idênticas às não addressable.** Uma string simples se resolve sozinha,
então a família `*AddressableAsync` foi removida em vez de renomeada:

```cs
MySceneManager.TransitionAsync("target", "loading");                     // Build Settings
MySceneManager.TransitionAsync("target-address", "loading-address");     // Addressables
MySceneManager.TransitionAsync(SceneRef.Address("target"), "loading");   // forçado, e o caminho rápido
```

**Não há camada de compatibilidade** — nenhum shim `[Obsolete]`, nenhum método de encaminhamento. Isso segue
o que a 3.0 e a 4.0 fizeram, e significa que cada chamada que precisa mudar produz um simples
erro de compilação exatamente na linha a ser alterada. **A 4.x não receberá mais manutenção**; a resposta
a um relato de bug na 4.x é atualizar.

:::warning[Usuários da Asset Store]
Remova completamente a versão anterior antes de importar a 5.0. Essa sempre foi a regra, mas em uma
versão major a chance de dar problema é maior.
:::

## Principais mudanças {/* #key-changes */}

* **64 métodos async públicos viraram 4.** Todo tipo de referência, quantidade de cenas e host fica acessível pelas
  conversões implícitas de `SceneParameters`, em vez de por um método próprio.
* **`SceneRef` substitui `ILoadSceneInfo`** e as cinco structs que a implementavam — um único value type sem boxing
  para nomes, caminhos, endereços, índices de build, `AssetReference`s e `Scene`s.
* **Uma `string` simples se resolve sozinha**, consultando primeiro as Build Settings e depois o Addressables.
* **Toda operação retorna uma `SceneOperation`** em vez de uma `Task<SceneResult>`: progresso,
  cancelamento, fase e eventos por cena vivem todos no handle.
* **`CancellationToken` e `IProgress<float>` saíram da API pública.**
* **`ISceneBackend` substitui `ISceneData` e `IAsyncSceneOperation`**, então a seleção de backend
  acontece uma vez por operação, em vez de a cada chamada.
* **Telas de carregamento não precisam mais ser cenas** — `LoadingScreen` também cobre prefabs e documentos
  do UI Toolkit.
* **Gates da tela de carregamento são retenções, não toggles.** `waitForScriptedStart` / `waitForScriptedEnd`
  e `StartTransition()` / `EndTransition()` foram removidos; um componente que precisa que a transição
  espere faz uma retenção no `LoadingProgress` e a libera quando terminar.
* **`LoadingScreenComponent`** é a base para tudo que vive em uma tela de carregamento. A
  referência ao `LoadingBehavior` é opcional e, quando ausente, é buscada nos pais.
* **`SceneManagerLog`** dá ao pacote uma única camada de logging configurável e roteável.
* **Consultas de cena respondem em vez de lançar exceção.** `GetLoadedSceneAt` e `GetLoadedSceneByName`
  viraram `TryGetLoadedSceneAt` / `TryGetLoadedSceneByName`.
* **`LoadingFader` faz o fade em tempo não escalado e limitado**, com `fadeInTime` / `fadeOutTime`
  separados, e `MinimumDisplayTime` é um componente do pacote.
* **Corrigido:** `LoadingProgress` não lança mais exceção quando uma transição é iniciada duas vezes — liberar uma
  retenção duas vezes é inofensivo.

## Tipos removidos e seus substitutos {/* #removed-types-and-their-replacements */}

Esta é a tabela para ler primeiro. Renomeações de método se resolvem com uma tecla no IntelliSense; tipos removidos,
não — `LoadSceneInfoName` não autocompleta para `SceneRef`.

### `ILoadSceneInfo` e as structs `LoadSceneInfo*` → `SceneRef` {/* #iloadsceneinfo-and-the-loadsceneinfo-structs--sceneref */}

Também cobre `LoadSceneInfoType`.

```cs
// 4.x
ILoadSceneInfo byName    = new LoadSceneInfoName("sceneA");
ILoadSceneInfo byPath    = new LoadSceneInfoName("Assets/Scenes/sceneA.unity");
ILoadSceneInfo byIndex   = new LoadSceneInfoIndex(1);
ILoadSceneInfo byScene   = new LoadSceneInfoScene(someScene);
ILoadSceneInfo byAddress = new LoadSceneInfoAddress("sceneA");
ILoadSceneInfo byAsset   = new LoadSceneInfoAssetReference(assetReference);

// 5.x
SceneRef byName    = "sceneA";                          // implícito
SceneRef byPath    = "Assets/Scenes/sceneA.unity";      // implícito
SceneRef byIndex   = 1;                                 // implícito
SceneRef byScene   = someScene;                         // implícito
SceneRef byAddress = SceneRef.Address("sceneA");        // explícito: força o Addressables
SceneRef byAsset   = assetReference;                    // implícito
```

Na maior parte do tempo você nem vai escrever `SceneRef` — as conversões significam que você passa a string,
o índice ou o `AssetReference` direto para a operação.

### `ISceneData`, `SceneData*`, `SceneDataBuilder`, `SceneDataUtilities` → `ISceneBackend` {/* #iscenedata-scenedata-scenedatabuilder-scenedatautilities--iscenebackend */}

Também cobre `IAsyncSceneOperation`, `AsyncSceneOperationStandard` e
`AsyncSceneOperationAddressable`.

```cs
// 4.x — meio implementado por design: cada tipo avisava quando você chamava a metade errada
public interface ISceneData
{
    IAsyncSceneOperation AsyncOperation { get; }
    void SetSceneReferenceManually(Scene scene);   // avisa na implementação addressable
    void UpdateSceneReference();                   // avisa na implementação padrão
    // ...
}

// 5.x — todo método tem significado em toda implementação
public interface ISceneBackend
{
    bool CanHandle(SceneRefKind kind);
    SceneBackendHandle Load(SceneRef sceneRef);
    SceneBackendHandle Unload(SceneBackendHandle handle);
    float GetProgress(SceneBackendHandle handle);
    bool IsDone(SceneBackendHandle handle);
    bool TryResolveScene(SceneBackendHandle handle, out Scene scene);
}
```

Registre o seu com `SceneBackendRegistry.Register(backend)`; ele tem precedência sobre os
backends embutidos para os tipos que declarar suportar.

### `WaitTask<T>` e `TaskExtensions` → `SceneOperation.ToCoroutine()` {/* #waittaskt-and-taskextensions--sceneoperationtocoroutine */}

```cs
// 4.x
yield return MySceneManager.LoadAsync("sceneA").ToWaitTask();
yield return new WaitTask<SceneResult>(MySceneManager.LoadAsync("sceneA"));

// 5.x
yield return MySceneManager.LoadAsync("sceneA").ToCoroutine();
```

### `SceneManagerExtensions` → removido {/* #scenemanagerextensions--deleted */}

As 698 linhas de métodos de extensão existiam para enumerar cada combinação de operação, quantidade de cenas e
tipo de referência. As conversões implícitas de `SceneParameters` substituem todos eles; veja a tabela de métodos
abaixo.

### `waitForScriptedStart` / `waitForScriptedEnd` e `StartTransition()` / `EndTransition()` → retenções {/* #waitforscriptedstart--waitforscriptedend-and-starttransition--endtransition--holds */}

Na 4.x, uma tela de carregamento que animava a entrada ou a saída marcava dois toggles no `LoadingBehavior` e
chamava dois gatilhos no seu `LoadingProgress` — e se dois componentes quisessem controlar o gate da mesma
transição, o primeiro a chamar `EndTransition()` liberava para os dois. Na 5.x os gates ficam
**abertos a menos que algo os retenha**: cada participante faz a sua própria retenção e o gate abre
quando o último deles libera.

```cs
// 4.x — waitForScriptedStart e waitForScriptedEnd marcados no Inspector
void Awake()
{
    _loadingBehavior.Progress.LoadingCompleted += PlayOut;
    PlayIn();
}
void OnPlayInFinished()  => _loadingBehavior.Progress.StartTransition();
void OnPlayOutFinished() => _loadingBehavior.Progress.EndTransition();

// 5.x — nada para marcar; as retenções são a declaração de que a transição deve esperar
void Awake()
{
    _loadingBehavior.Progress.HoldShow(this);
    _loadingBehavior.Progress.HoldHide(this);
    _loadingBehavior.Progress.LoadingCompleted += PlayOut;
    PlayIn();
}
void OnPlayInFinished()  => _loadingBehavior.Progress.ReleaseShow(this);
void OnPlayOutFinished() => _loadingBehavior.Progress.ReleaseHide(this);
```

Faça as retenções no `Awake` ou no `OnEnable`, antes de a transição ler os gates. Um novo
par `HoldCompletion` / `ReleaseCompletion` atrasa o próprio sinal de `LoadingCompleted`, que é exatamente o que
um tempo mínimo de exibição precisa. Veja [Gates e retenções](../getting-started/loading-screens.md#gates-and-holds).

O `LoadingFader` agora faz suas próprias retenções, então uma cena que só usava ele funciona sem nenhuma mudança
além dos toggles que desaparecem do Inspector.

### `LoadingProgress.TransitionInTask` / `TransitionOutTask` → `WaitForShowAsync()` / `WaitForHideAsync()` {/* #loadingprogresstransitionintask--transitionouttask--waitforshowasync--waitforhideasync */}

Esses eram campos públicos `TaskCompletionSource<bool>`, então qualquer consumidor podia completá-los e
dessincronizar a transição. Se você os lia para descobrir quando uma transição terminava uma
fase, use `SceneOperation.StateChanged` no lugar — veja [Observando uma transição](#watching-a-transition).

```cs
// 4.x
await loadingBehavior.Progress.TransitionInTask.Task;

// 5.x
await loadingBehavior.Progress.WaitForShowAsync();
bool shown = loadingBehavior.Progress.IsShown;
```

### Campo `loadingBehavior` dos componentes de feedback → `LoadingScreenComponent.LoadingBehavior` {/* #feedback-components-loadingbehavior-field--loadingscreencomponentloadingbehavior */}

`LoadingFader`, `LoadingFeedbackSlider`, `LoadingFeedbackText` e `LoadingFeedbackTextMeshPro`
agora estendem `LoadingScreenComponent`. O campo público `loadingBehavior` deles virou uma propriedade
`LoadingBehavior`, mantida serializada com o nome antigo para que cenas existentes preservem suas ligações — e ela é
opcional, resolvida a partir do mesmo objeto ou do pai mais próximo quando deixada vazia.

```cs
// 4.x
slider.loadingBehavior = behavior;

// 5.x — ou deixe vazio e coloque o LoadingBehavior em um pai
slider.LoadingBehavior = behavior;
```

Se você escreveu seu próprio feedback em cima de `LoadingBehavior.Progress`, estenda `LoadingScreenComponent`
no lugar e mova a inscrição para o `OnBound`:

```cs
// 4.x
public class LoadingFeedbackImageFill : MonoBehaviour
{
    public LoadingBehavior loadingBehavior;
    void Start() => loadingBehavior.Progress.Progressed += p => _image.fillAmount = p;
}

// 5.x
public class LoadingFeedbackImageFill : LoadingScreenComponent
{
    protected override void OnBound() => Progress.Progressed += p => _image.fillAmount = p;
}
```

### `GetLoadedSceneAt` / `GetLoadedSceneByName` → `TryGetLoadedSceneAt` / `TryGetLoadedSceneByName` {/* #getloadedsceneat--getloadedscenebyname--trygetloadedsceneat--trygetloadedscenebyname */}

Os dois lançavam exceção quando nada correspondia, então "essa cena está carregada?" só podia ser
perguntado por meio de uma exceção. As formas `Try` respondem, e a versão por índice checa os limites
explicitamente — `LoadedSceneCount` muda enquanto outros carregamentos e descarregamentos rodam, então
é seguro percorrê-la.

```cs
// 4.x
try { var hud = sceneManager.GetLoadedSceneByName("HUD"); }
catch (ArgumentException) { /* não carregada */ }

// 5.x
if (sceneManager.TryGetLoadedSceneByName("HUD", out Scene hud))
    hud.GetRootGameObjects();
```

`TryGetLoadedSceneByName` enxerga cenas que **terminaram** de carregar, então não serve de proteção
contra iniciar um segundo carregamento da mesma cena. Para isso, guarde a `SceneOperation` que o
primeiro `LoadAsync` retornou.

### `LoadingFader.fadeTime` → `fadeInTime` / `fadeOutTime` {/* #loadingfaderfadetime--fadeintime--fadeouttime */}

O único `fadeTime` virou dois campos. Telas existentes migram sozinhas: o valor serializado cai em
`fadeInTime`, então o fade para o qual elas foram ajustadas mantém o tempo, e `fadeOutTime` começa
no padrão de um segundo.

Os fades também rodam agora em tempo **não escalado e limitado**. Uma transição iniciada a partir de
um jogo pausado não trava mais em `timeScale = 0`, e um único frame longo — a cena sendo ativada —
avança um fade no máximo `maxFrameStep` (1/30 s por padrão), em vez de consumi-lo antes que qualquer
coisa seja desenhada.

`MinimumDisplayTime` saiu do exemplo Loading Scene Examples e entrou no pacote, em
`MyGameDevTools.SceneLoading`, então um jogo pode depender dele sem copiar o arquivo. Se você tinha
copiado a versão do exemplo, apague a sua cópia — as duas têm o mesmo nome. O campo `_seconds` virou
um `seconds` público; valores serializados são preservados.

## Todo método da 4.x e seu equivalente na 5.x {/* #every-4x-method-and-its-5x-equivalent */}

Cada grupo começa pelo caso que não muda.

### Load {/* #load */}

| 4.x | 5.x |
|---|---|
| `LoadAsync(sceneParameters, progress, token)` | `LoadAsync(sceneParameters)` + `op.Progressed` / `op.CancelWith(token)` |
| `LoadAsync(string sceneName, bool setActive, ...)` | `LoadAsync(sceneName)` — ou `LoadAsync(new SceneParameters(sceneName, setActive: true))` |
| `LoadAsync(string[] sceneNames, int setIndexActive, ...)` | `LoadAsync(sceneNames)` — ou `LoadAsync(new SceneParameters(sceneNames, setIndexActive))` |
| `LoadAsync(int buildIndex, bool setActive, ...)` | `LoadAsync(buildIndex)` — ou `LoadAsync(new SceneParameters((SceneRef)buildIndex, true))` |
| `LoadAsync(int[] buildIndices, int setIndexActive, ...)` | `LoadAsync(buildIndices)` — ou `LoadAsync(new SceneParameters(buildIndices, setIndexActive))` |
| `LoadAddressableAsync(string address, bool setActive, ...)` | `LoadAsync(SceneRef.Address(address))` |
| `LoadAddressableAsync(string[] addresses, int setIndexActive, ...)` | `LoadAsync(new SceneParameters(addresses.Select(SceneRef.Address).ToArray(), setIndexActive))` |
| `LoadAddressableAsync(AssetReference assetReference, bool setActive, ...)` | `LoadAsync(assetReference)` |
| `LoadAddressableAsync(AssetReference[] assetReferences, int setIndexActive, ...)` | `LoadAsync(assetReferences)` — ou `LoadAsync(new SceneParameters(assetReferences, setIndexActive))` |

Um endereço simples só precisa de `SceneRef.Address(...)` quando o mesmo nome também existe nas suas Build
Settings; caso contrário, `LoadAsync(address)` resolve para o Addressables por conta própria.

### Unload {/* #unload */}

| 4.x | 5.x |
|---|---|
| `UnloadAsync(sceneParameters, token)` | `UnloadAsync(sceneParameters)` |
| `UnloadAsync(string sceneName, token)` | `UnloadAsync(sceneName)` |
| `UnloadAsync(string[] sceneNames, token)` | `UnloadAsync(sceneNames)` |
| `UnloadAsync(int buildIndex, token)` | `UnloadAsync(buildIndex)` |
| `UnloadAsync(int[] buildIndices, token)` | `UnloadAsync(buildIndices)` |
| `UnloadAsync(Scene scene, token)` | `UnloadAsync(scene)` |
| `UnloadAsync(Scene[] scenes, token)` | `UnloadAsync(scenes)` |
| `UnloadAddressableAsync(string address, token)` | `UnloadAsync(SceneRef.Address(address))` |
| `UnloadAddressableAsync(string[] addresses, token)` | `UnloadAsync(addresses.Select(SceneRef.Address).ToArray())` |
| `UnloadAddressableAsync(AssetReference assetReference, token)` | `UnloadAsync(assetReference)` |
| `UnloadAddressableAsync(AssetReference[] assetReferences, token)` | `UnloadAsync(assetReferences)` |

### Transition {/* #transition */}

| 4.x | 5.x |
|---|---|
| `TransitionAsync(sceneParameters, intermediateSceneReference, token)` | `TransitionAsync(sceneParameters, loadingScreen)` |
| `TransitionAsync(string target, string loading, token)` | `TransitionAsync(target, loading)` — **inalterado** |
| `TransitionAsync(string[] targets, string loading, int setIndexActive, token)` | `TransitionAsync(new SceneParameters(targets, setIndexActive), loading)` |
| `TransitionAsync(int target, int loading, token)` | `TransitionAsync(target, loading)` — **inalterado** |
| `TransitionAsync(int[] targets, int loading, int setIndexActive, token)` | `TransitionAsync(new SceneParameters(targets, setIndexActive), loading)` |
| `TransitionAddressableAsync(string target, string loading, token)` | `TransitionAsync(SceneRef.Address(target), SceneRef.Address(loading))` |
| `TransitionAddressableAsync(string[] targets, string loading, int setIndexActive, token)` | `TransitionAsync(new SceneParameters(targets.Select(SceneRef.Address).ToArray(), setIndexActive), SceneRef.Address(loading))` |
| `TransitionAddressableAsync(AssetReference target, AssetReference loading, token)` | `TransitionAsync(target, loading)` |
| `TransitionAddressableAsync(AssetReference[] targets, AssetReference loading, int setIndexActive, token)` | `TransitionAsync(new SceneParameters(targets, setIndexActive), loading)` |

`setIndexActive` tinha `0` como padrão em toda sobrecarga de transição da 4.x, e uma transição continua
ativando o índice 0 a menos que você diga o contrário — então remover o argumento mantém o mesmo comportamento.

### Reload {/* #reload */}

| 4.x | 5.x |
|---|---|
| `ReloadActiveSceneAsync(intermediateSceneReference, token)` | `ReloadActiveSceneAsync(loadingScreen)` |
| `ReloadActiveSceneAsync(string loadingSceneName, token)` | `ReloadActiveSceneAsync(loadingSceneName)` — **inalterado** |
| `ReloadActiveSceneAsync(int loadingBuildIndex, token)` | `ReloadActiveSceneAsync(loadingBuildIndex)` — **inalterado** |
| `ReloadActiveSceneAddressableAsync(string loadingAddress, token)` | `ReloadActiveSceneAsync(SceneRef.Address(loadingAddress))` |
| `ReloadActiveSceneAddressableAsync(AssetReference loadingAssetReference, token)` | `ReloadActiveSceneAsync(loadingAssetReference)` |

## Await, progresso e cancelamento {/* #awaiting-progress-and-cancellation */}

Tudo que antes era um argumento agora é algo que você anexa ao handle.

```cs
// 4.x
var progress = new Progress<float>(p => bar.value = p);
var cts = new CancellationTokenSource();
Task<SceneResult> task = MySceneManager.LoadAsync("sceneA", progress: progress, token: cts.Token);
SceneResult result = await task;

// 5.x
SceneOperation op = MySceneManager.LoadAsync("sceneA");
op.Progressed += p => bar.value = p;
SceneResult result = await op;
```

`await op` não precisa de `Task`. Se você precisar de uma para interoperar com bibliotecas de terceiros, `op.AsTask()` entrega uma.

O cancelamento agora tem um único mecanismo:

```cs
op.Cancel();                              // interrompe esta operação
op.CancelWith(destroyCancellationToken);  // ponte opcional para concorrência estruturada
```

:::note
Operações de cena da Unity não podem ser abortadas — a própria documentação da 4.x dizia isso em todos os 64 métodos, e
o token só cancelava o *await*. `Cancel()` para de reportar progresso, pula as
fases restantes e completa a operação em `Canceled`; o carregamento subjacente ainda termina.
:::

## Observando uma transição {/* #watching-a-transition */}

Uma `SceneOperation` reporta em qual fase está, o que antes exigia entrar em um
`LoadingBehavior` e chamar `ContinueWith` em um `TaskCompletionSource` exposto publicamente:

```cs
SceneOperation op = MySceneManager.TransitionAsync("target", "loading");

op.StateChanged += o =>
{
    if (o.State == SceneOperationState.ScreenOut)
        BeginIntroAnimation();    // a tela de carregamento terminou de se esconder
};

await op;
```

Os estados seguem `Pending → Resolving → ScreenIn → Unloading → Loading → Activating → ScreenOut →
Completed`, e uma operação pula as fases que não fazem sentido para o seu tipo.

## Telas de carregamento personalizadas {/* #custom-loading-screens */}

Uma tela de carregamento não precisa mais ser uma cena. Tudo que funcionava na 4.x continua funcionando — um nome
de cena, caminho, endereço, índice de build, `Scene` ou `AssetReference` são todos convertidos em uma tela baseada em cena
— e agora você pode escrever a sua própria:

```cs
public class MyScreen : LoadingScreen
{
    public override SceneOperationPump.ConditionAwaiter PrepareAsync(LoadingScreenHost host, SceneOperation op)
    {
        /* instancie dentro do host e então faça BindProgress(...) do LoadingProgress que controla o gate */
        return SceneOperationPump.Completed(op);
    }

    public override void Dispose() { /* desmonte tudo */ base.Dispose(); }
}

await MySceneManager.TransitionAsync("target", new MyScreen());
```

`PrepareAsync` é o único membro que uma tela precisa implementar, além de `Dispose` se ela construiu algo.
Exibir, esconder e reportar progresso são conduzidos pelo `LoadingProgress` ao qual a tela se vincula — um encontrado em um
`LoadingBehavior`, ou um que ela mesma cria — então toda tela controla o gate do mesmo jeito.

`LoadingScreenHost` é uma cena de propriedade do pacote que existe durante uma transição, então uma
tela que instancia algo tem onde colocá-lo sem que ele se perca quando a cena de saída for
descarregada. Ela também substitui a `temp-transition-scene` interna da 4.x.

O exemplo [Loading Scene Examples](../samples/loading-scene-examples.md) inclui `PrefabLoadingScreen`
e `UIDocumentLoadingScreen` como implementações de referência para copiar.

:::note[O exemplo foi reconstruído]
As cenas `Loading_Fade` e `Loading_Custom` e os scripts `SceneTransitionTrigger`,
`AnimatedTrigger` e `LoadingFeedbackImageFill` do exemplo da 4.x foram removidos. Se você copiou algum deles para o
seu projeto, saiba que eles foram escritos em cima dos toggles e gatilhos removidos — reimporte o exemplo
e parta dos scripts da 5.x.
:::

## Resolução de strings e sua precedência {/* #string-resolution-and-its-precedence */}

Uma string simples é resolvida quando a operação começa:

1. **Build Settings**, por nome ou caminho. Uma única consulta em dicionário, síncrona — e o caso comum.
2. **Addressables**, se as Build Settings não a tiverem e o Addressables estiver instalado. Isso
   precisa do catálogo, então é assíncrono — o primeiro carregamento addressable por string paga a
   latência de inicialização do catálogo, e os carregamentos seguintes, de qualquer chave, vêm do cache.
3. **Nenhum dos dois** → uma exceção que cita os dois lugares onde procuramos.

**As Build Settings vencem.** Se `Level1` existe nos dois, `LoadAsync("Level1")` carrega a versão das Build
Settings, e `SceneRef.Address("Level1")` é a forma de sobrescrever isso.

:::warning[A resolução é um comportamento observável]
Adicionar uma cena às Build Settings mais tarde pode mudar uma string do backend addressable para o
padrão, sem nenhuma alteração no código. Uma chave que corresponde aos dois é reportada no nível `Warning`, e toda
primeira resolução é registrada em `Verbose`, então o problema é diagnosticável, não misterioso.
:::

## Logging {/* #logging */}

O pacote agora tem uma única camada de logging em vez de nove chamadas `Debug.LogWarning` espalhadas.

```cs
SceneManagerLog.Level = SceneLogLevel.Verbose;   // Off | Error | Warning | Info | Verbose
SceneManagerLog.Handler = myLogHandler;          // redirecione para um console dentro do jogo ou para analytics
```

O padrão é `Warning` em builds de desenvolvimento e `Error` em release, e pode ser alterado em tempo de execução,
então dá para elevar o nível em um build publicado para diagnosticar um problema em produção. Defina `MSM_DISABLE_LOGGING` para remover
a camada por completo.

`Verbose` é onde a camada de vinculação de cenas narra o que está fazendo — qual referência resolveu para o quê e
qual cena carregada foi vinculada a qual referência. Historicamente, essa é a parte mais delicada do
pacote, então vale a pena ligar quando alguma vinculação sai errada.

## Uma nota sobre progresso {/* #a-note-on-progress */}

Progresso significa algo ligeiramente diferente em cada backend, e sempre foi assim. O progresso do Addressables abrange
download, carregamento e ativação; o caminho padrão cobre apenas o carregamento. Um grupo que mistura os dois,
portanto, avança de forma desigual. Isso é documentado, não corrigido — reescalar um para bater com o
outro seria inventar um número que nenhum dos dois backends reporta.
