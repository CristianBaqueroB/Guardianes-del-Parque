using UnityEngine;

[RequireComponent(typeof(PlayerInventory))]
public class PlayerInteraction : MonoBehaviour
{
    private PlayerInventory inventory;
    private Animator anim;

    private TrashData currentTrash = null;
    private RecycleBin currentBin = null;

    private void Awake()
    {
        inventory = GetComponent<PlayerInventory>();
        if (inventory == null)
        {
            inventory = gameObject.AddComponent<PlayerInventory>();
        }

        anim = GetComponent<Animator>();
        if (anim == null)
        {
            anim = GetComponentInChildren<Animator>();
        }

        // Asegura que exista el HUD en la escena automáticamente
        if (InteractionHUD.Instance == null && FindAnyObjectByType<InteractionHUD>() == null)
        {
            GameObject hudObj = new GameObject("InteractionHUD_Auto");
            hudObj.AddComponent<InteractionHUD>();
        }

        // Asegura que exista el gestor de misiones y pureza ambiental
        if (EcoMissionManager.Instance == null && FindAnyObjectByType<EcoMissionManager>() == null)
        {
            GameObject ecoObj = new GameObject("EcoMissionManager_Auto");
            ecoObj.AddComponent<EcoMissionManager>();
        }

        // Asegura que exista la mini-brújula de canecas en el jugador
        if (BinCompass.Instance == null && GetComponent<BinCompass>() == null)
        {
            gameObject.AddComponent<BinCompass>();
        }

        // Asegura que el jugador tenga sistema de vida
        if (GetComponent<PlayerHealth>() == null)
        {
            gameObject.AddComponent<PlayerHealth>();
        }
    }

    private void Start()
    {
        if (inventory != null && InteractionHUD.Instance != null)
        {
            inventory.OnInventoryChanged += OnInventoryChanged;
            InteractionHUD.Instance.UpdateInventoryDisplay(inventory);
        }
    }

    private void OnDestroy()
    {
        if (inventory != null)
        {
            inventory.OnInventoryChanged -= OnInventoryChanged;
        }
    }

    private void OnInventoryChanged()
    {
        if (InteractionHUD.Instance != null)
        {
            InteractionHUD.Instance.UpdateInventoryDisplay(inventory);
        }
    }

    private void Update()
    {
        // Ignorar interacciones si el juego está en pausa
        if (GameManager.instance != null && GameManager.instance.isPaused) return;

        HandleInventoryCycling();
        UpdatePrompt();

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (currentBin != null)
            {
                InteractWithBin();
            }
            else if (currentTrash != null)
            {
                InteractWithTrash();
            }
        }
    }

    private void HandleInventoryCycling()
    {
        if (inventory == null || inventory.CurrentCount <= 1) return;

        // Tecla Q para ciclar residuo activo
        if (Input.GetKeyDown(KeyCode.Q))
        {
            inventory.CycleNext();
        }

        // Rueda del ratón para ciclar
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0.05f)
        {
            inventory.CyclePrevious();
        }
        else if (scroll < -0.05f)
        {
            inventory.CycleNext();
        }

        // Teclas 1, 2, 3 para selección directa
        if (Input.GetKeyDown(KeyCode.Alpha1)) inventory.SelectSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) inventory.SelectSlot(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) inventory.SelectSlot(2);
    }

    private void UpdatePrompt()
    {
        if (InteractionHUD.Instance == null) return;

        if (currentBin != null)
        {
            if (inventory.IsEmpty)
            {
                InteractionHUD.Instance.ShowPrompt($"{currentBin.binName} (Mochila vacía - Recoge residuos primero)");
            }
            else
            {
                CarriedTrash selected = inventory.SelectedItem;
                string trashInfo = (selected != null) ? $"<b>{selected.name}</b>" : "residuo";
                string cycleHint = (inventory.CurrentCount > 1) ? " [Q: Cambiar]" : "";
                InteractionHUD.Instance.ShowPrompt($"Depositar {trashInfo} en {currentBin.binName}{cycleHint}");
            }
        }
        else if (currentTrash != null)
        {
            if (inventory.IsFull)
            {
                InteractionHUD.Instance.ShowPrompt("Mochila Llena (Llévala a un Punto Limpio)");
            }
            else
            {
                InteractionHUD.Instance.ShowPrompt($"Recoger {currentTrash.trashName} [{currentTrash.GetCategoryDisplayName()}]");
            }
        }
        else
        {
            InteractionHUD.Instance.HidePrompt();
        }
    }

    private void InteractWithTrash()
    {
        if (currentTrash == null) return;

        if (inventory.IsFull)
        {
            if (InteractionHUD.Instance != null)
            {
                InteractionHUD.Instance.ShowNotification("¡Mochila llena! Lleva los residuos al Punto Limpio.", null, false, 3f);
            }
            return;
        }

        // Dispara la animación de recolección de forma segura si el parámetro existe
        TriggerPickUpAnimation();

        TrashData trashToCollect = currentTrash;
        currentTrash = null;

        inventory.TryAddTrash(trashToCollect);

        if (InteractionHUD.Instance != null)
        {
            string cycleHint = (inventory.CurrentCount > 1) ? " (Presiona [Q] para cambiar residuo en mano)" : "";
            InteractionHUD.Instance.ShowNotification(
                $"Recogiste: <b>{trashToCollect.trashName}</b> ({trashToCollect.GetCategoryDisplayName()}){cycleHint}",
                trashToCollect.educationalFact,
                true,
                3.5f
            );
        }

        // Notificar al gestor de misiones
        if (EcoMissionManager.Instance != null)
        {
            EcoMissionManager.Instance.OnTrashCollected(trashToCollect);
        }

        Destroy(trashToCollect.gameObject);
    }

    private void TriggerPickUpAnimation()
    {
        if (anim == null) return;

        // Comprobación de seguridad para no lanzar error si 'PickUp' no existe en el Animator
        foreach (AnimatorControllerParameter param in anim.parameters)
        {
            if (param.name == "PickUp")
            {
                anim.SetTrigger("PickUp");
                break;
            }
        }
    }

    private void InteractWithBin()
    {
        if (currentBin == null) return;

        if (inventory.IsEmpty)
        {
            if (InteractionHUD.Instance != null)
            {
                InteractionHUD.Instance.ShowNotification("¡Mochila vacía! Primero recoge residuos del parque.", null, false, 2.5f);
            }
            return;
        }

        CarriedTrash item = inventory.ExtractSelectedItem();
        if (item == null) return;

        RecycleBin.DepositResult result = currentBin.ProcessDeposit(item);

        if (GameManager.instance != null)
        {
            int basePoints = currentBin.correctBonusPoints + item.points;
            float baseBonusTime = currentBin.timeBonus + item.timeBonus;
            GameManager.instance.RegisterDeposit(basePoints, baseBonusTime, result.isCorrect);
        }

        if (EcoMissionManager.Instance != null)
        {
            int streak = (GameManager.instance != null) ? GameManager.instance.currentStreak : 0;
            EcoMissionManager.Instance.OnTrashDeposited(item.category, result.isCorrect, streak);
        }

        if (InteractionHUD.Instance != null)
        {
            InteractionHUD.Instance.ShowNotification(
                result.message,
                result.educationalFact,
                result.isCorrect,
                4f
            );

            InteractionHUD.Instance.TriggerScreenFlash(result.isCorrect);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        RecycleBin bin = other.GetComponent<RecycleBin>();
        if (bin == null) bin = other.GetComponentInParent<RecycleBin>();

        if (bin != null)
        {
            currentBin = bin;
            return;
        }

        if (other.CompareTag("Trash"))
        {
            TrashData data = other.GetComponent<TrashData>();
            if (data == null) data = other.GetComponentInParent<TrashData>();

            if (data != null)
            {
                currentTrash = data;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        RecycleBin bin = other.GetComponent<RecycleBin>();
        if (bin == null) bin = other.GetComponentInParent<RecycleBin>();

        if (bin != null && currentBin == bin)
        {
            currentBin = null;
            return;
        }

        if (other.CompareTag("Trash"))
        {
            TrashData data = other.GetComponent<TrashData>();
            if (data == null) data = other.GetComponentInParent<TrashData>();

            if (data != null && currentTrash == data)
            {
                currentTrash = null;
            }
        }
    }
}