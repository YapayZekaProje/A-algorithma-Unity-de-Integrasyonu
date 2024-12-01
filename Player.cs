using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Player : MonoBehaviour
{
    public TextMeshProUGUI speedText; // Oyuncunun hızını göstermek için UI elemanı

    public float currentSpeed; // Oyuncunun mevcut hızı
    private float moveSpeed = 5; // Oyuncunun manuel kontrol hızını belirler
    public float maxSpeed; // Oyuncunun ulaşabileceği maksimum hız
    public float deceleration = 0.2f; // Yavaşlama oranı
    public float acceleration = 0.5f;  // Hızlanma oranı
    public bool isSlowingDown = false; // Oyuncunun yavaşlayıp yavaşlamadığını belirler
    public bool isAccelerating = true; // Oyuncunun hızlanıp hızlanmadığını belirler

    private Vector3 targetPosition; // Oyuncunun gitmesi gereken hedef pozisyon
    private int currentNodeIndex = 0; // Şu anda üzerinde bulunulan yol düğümünün indeksi
    private List<Node> path; // Oyuncunun takip edeceği yol

    // Yolun bitip bitmediğini kontrol eden metot
    public bool IsPathFinished()
    {
        return path == null || currentNodeIndex >= path.Count;
    }

    // Oyuncunun takip edeceği yolu ayarlayan metot
    public void SetPath(List<Node> newPath)
    {
        path = newPath; // Yeni yolu belirle
        currentNodeIndex = 0; // İlk düğümden başla
        if (path.Count > 0)
        {
            targetPosition = path[currentNodeIndex].WorldPosition; // İlk hedef pozisyonu ayarla
        }
    }

    // Oyuncuyu hedefe doğru hareket ettiren metot
    public void MoveToTarget(Vector3 target)
    {
        LookToTarget(target); // Hedefe dön
        transform.position = Vector3.MoveTowards(transform.position, target, currentSpeed * Time.deltaTime); // Hedefe doğru hareket et
    }

    private void Update()
    {
        // Oyuncunun takip edebileceği bir yol varsa hedef pozisyona doğru hareket et
        if (path != null && path.Count > 0)
        {
            // Hızlanma veya yavaşlama durumuna göre hız güncellemesi
            if (isAccelerating)
            {
                currentSpeed = Mathf.Min(maxSpeed, currentSpeed + acceleration * Time.deltaTime); // Maksimum hızı aşma
            }
            else if (isSlowingDown)
            {
                currentSpeed = Mathf.Max(0, currentSpeed - deceleration * Time.deltaTime); // Hızı sıfırın altına düşürme
            }

            MoveToTarget(targetPosition); // Hedefe doğru hareket et

            // Hedef düğüme ulaşılıp ulaşılmadığını kontrol et
            if (Vector3.Distance(transform.position, targetPosition) < 0.1f) // Eşik değeri (mesafe kontrolü)
            {
                currentNodeIndex++; // Sonraki düğüme geç
                if (currentNodeIndex < path.Count)
                {
                    targetPosition = path[currentNodeIndex].WorldPosition; // Yeni hedef pozisyonu güncelle
                }
                else
                {
                    Debug.Log("Yolun sonuna ulaşıldı!"); // Yolun bittiğini bildir
                    path = null; // Yol bilgisini sıfırla
                }
            }
        }

        GameControl(); // Manuel kontrolü işleyen metot
    }

    // Oyuncuyu hedefe bakacak şekilde döndür
    public void LookToTarget(Vector3 target)
    {
        Vector3 direction = target - transform.position; // Hedefe doğru yön

        // Eğer hedef çok yakın değilse
        if (direction.magnitude > 0.1f)
        {
            direction.y = 0;  // Y ekseninde döndürmeyi engelle

            Quaternion targetRotation = Quaternion.LookRotation(direction); // Hedef yönüne doğru dön

            // Yumuşak bir dönüş sağla
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 2.0f);
        }
    }

    // Oyunun manuel kontrollerini işleyen metot
    private void GameControl()
    {
        Vector3 newPosition = transform.position;

        // Klavye ok tuşlarıyla hareketi kontrol et
        if (Input.GetKey(KeyCode.UpArrow))
        {
            newPosition.z += moveSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            newPosition.z -= moveSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            newPosition.x += moveSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            newPosition.x -= moveSpeed * Time.deltaTime;
        }

        // Pozisyonu güncelle
        transform.position = newPosition;

        // Hız kontrolü
        if (maxSpeed == currentSpeed)
        {
            isAccelerating = false; // Maksimum hıza ulaşıldıysa hızlanmayı durdur
        }
        if (maxSpeed != currentSpeed)
        {
            isAccelerating = true; // Maksimum hızdan düşükse hızlanmaya devam et
        }

        SpeedTextUpdate(); // Hız göstergesini güncelle
    }

    // Hız bilgisini kullanıcı arayüzünde güncelleyen metot
    private void SpeedTextUpdate()
    {
        speedText.text = (currentSpeed * 10).ToString("F1") + " Km/H"; // Mevcut hızı göster
    }
}
