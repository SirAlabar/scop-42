NAME    = scop
DOTNET  = dotnet

# ── Targets ───────────────────────────────────────────────────────────────── #

all: $(NAME)

$(NAME):
	$(DOTNET) publish -c Release -o ./build --nologo
	@ln -sf build/$(NAME) $(NAME)

clean:
	$(DOTNET) clean --nologo -v quiet
	rm -rf obj/

fclean: clean
	rm -rf bin/ build/
	rm -f $(NAME)

re: fclean all

.PHONY: all clean fclean re