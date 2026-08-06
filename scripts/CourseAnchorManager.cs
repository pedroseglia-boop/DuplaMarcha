using System.Collections;
using UnityEngine;

public class CourseAnchorManager : MonoBehaviour
{
    [Header("Referências da Cena")]
    [Tooltip("Arraste o objeto GameWorldRoot aqui")]
    public Transform gameWorldRoot;

    private OVRSpatialAnchor spatialAnchor;

    void Start()
    {
        // 1. Cria um objeto raiz para servir de âncora do percurso
        GameObject anchorObject = new GameObject("CourseAnchor");

        // 2. Posiciona a âncora na origem do percurso (idealmente baseada no chão físico)
        // Se usar o MRUK, você pode igualar esta posição à posição do MRUKRoom.FloorAnchor posteriormente
        anchorObject.transform.position = this.transform.position;
        anchorObject.transform.rotation = this.transform.rotation;

        // 3. Inicia o processo de ancoragem do Meta XR
        StartCoroutine(ConfigurarOVRSpatialAnchor(anchorObject));
    }

    private IEnumerator ConfigurarOVRSpatialAnchor(GameObject anchorObject)
    {
        // Adiciona o componente correto da Meta
        spatialAnchor = anchorObject.AddComponent<OVRSpatialAnchor>();

        Debug.Log("Aguardando o hardware do Quest criar a OVRSpatialAnchor...");

        // Aguarda ativamente a flag 'Created' se tornar verdadeira
        // Isso impede que o mundo seja atrelado a uma âncora inexistente ou inválida
        while (!spatialAnchor.Created)
        {
            yield return null;
        }

        Debug.Log("OVRSpatialAnchor criada com sucesso!");

        // 4. Ancoragem do cenário virtual
        if (gameWorldRoot != null)
        {
            // O mundo inteiro do jogo agora é subordinado a essa âncora fixa no mundo real
            gameWorldRoot.SetParent(anchorObject.transform);
            Debug.Log("GameWorldRoot ancorado fisicamente ao CourseAnchor.");
        }
        else
        {
            Debug.LogError("Falha: GameWorldRoot não foi atribuído no CourseAnchorManager!");
        }
    }
}