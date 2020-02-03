function factorial(i)
    if i == 1 then
        return 1
    end

    return i * factorial(i - 1)
end

print("Factorial of 3 6 And 9: ");

print(factorial(3))
print(factorial(6))
print(factorial(9))

print("Also you can do string concatenation. Everything is kinda buggy")
print("Hello " .. "Concatenated " ..  "World")