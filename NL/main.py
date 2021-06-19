# Необходимые библиотеки
import pytesseract
# import numpy as np
import imutils
import os
from sys import argv
import cv2
from PIL import Image, ImageEnhance


# Функция, которая вызывается в C# на сервере
def recognize_digits(image, sal, sar, lal, lar, pl, pr):
    """"Функция принимает path картинки, примерные диапазоны показателей пользователя с помощью обученной нейросети
        определяет и возвращает цифры с экрана. Точность примерно 70-80%"""
    im = Image.open(image)
    enhancer = ImageEnhance.Brightness(im)
    factor = 2.5
    im_output = enhancer.enhance(factor)  # Повышаем яркость
    im_output.save('roi.png')
    im = Image.open('roi.png')
    enhancer = ImageEnhance.Contrast(im)
    factor = 2
    im_output = enhancer.enhance(factor)  # Повышаем контрастность
    im_output.save('roi.png')
    img = cv2.imread("roi.png")
    img = imutils.resize(img, height=500)
    gray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)  # Красим в серый цвет
    thresh = cv2.threshold(gray, 0, 255, cv2.THRESH_BINARY_INV | cv2.THRESH_OTSU)[1]
    # Определяем контуры явно тёмных пикселей
    thresh = 255 - thresh  # Инвертируем цвета
    thresh = cv2.resize(thresh, (0, 0), fx=0.5, fy=0.5)  # Уменьшаем картинку в 0.5x
    # pytesseract.pytesseract.tesseract_cmd = r'C:\Program Files\Tesseract-OCR\tesseract.exe'
    data = pytesseract.image_to_string(thresh, lang='seg', config=r'--psm 6')  # Работа нейросетм
    os.remove('roi.png')
    os.remove(image)
    try:
        s1 = data.replace(" ", "")  # Удаляем лишние символы
        s1 = s1.replace("\n", "")
        s1 = s1.replace("\x0c", "")
    except Exception:
        pass
    sad = str
    lad = str
    pulse = str
    # Фильтруем определённые цифры
    for i in range(sal, sar):
        index = s1.find(str(i))
        if index != -1:
            sad = i
            break

    for i in range(lal, lar):
        index1 = s1.find(str(i), index)
        if index1 != -1:
            lad = i
            break

    for i in range(pl, pr):
        index2 = s1.find(str(i), index1 + 1)
        if index2 != -1:
            pulse = i
            break
    # Складываем всё и возвращаем
    data = str(sad) + " " + str(lad) + " " + str(pulse)
    print(data)
    # cv2.imshow("ROI", thresh)
    # cv2.waitKey()
    # return data


if __name__ == "__main__":
    script, image, sal, sar, lal, lar, pl, pr = argv
    recognize_digits(image, int(sal), int(sar), int(lal), int(lar), int(pl), int(pr))