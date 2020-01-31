function f()
  a = 5

  function b()
        print("wow")

        function c()
            print("no")

            return 0
        end

        return 0
  end

  return 0
end

i = 5

while 1 do

    if i == 0 then
        return 0 
    end

    print(i)
    i = i - 1
end